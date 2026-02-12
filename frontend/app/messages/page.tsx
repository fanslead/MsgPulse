'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

interface MessageRecord {
  id: number;
  taskId: string;
  messageType: string;
  templateCode: string;
  recipient: string;
  sendStatus: string;
  sendTime?: string;
  completeTime?: string;
  failureReason?: string;
  manufacturer?: { name: string };
  createdAt: string;
}

interface Template {
  id: number;
  code: string;
  name: string;
}

export default function MessagesPage() {
  const [activeTab, setActiveTab] = useState<'send' | 'batch' | 'records'>('send');
  const [messages, setMessages] = useState<MessageRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);

  // 单条发送表单
  const [sendForm, setSendForm] = useState({
    messageType: 'SMS',
    templateCode: '',
    recipient: '',
    variables: {} as Record<string, string>,
  });

  // 批量发送表单
  const [batchForm, setBatchForm] = useState({
    messageType: 'SMS',
    templateCode: '',
    recipients: '',
    variables: {} as Record<string, string>,
  });

  // 筛选条件
  const [filter, setFilter] = useState({
    messageType: '',
    sendStatus: '',
    startTime: '',
    endTime: '',
  });

  const [templates, setTemplates] = useState<Template[]>([]);
  const [emailTemplates, setEmailTemplates] = useState<Template[]>([]);

  useEffect(() => {
    loadMessages();
    loadTemplates();
  }, []);

  const showMessage = (type: 'success' | 'error', text: string) => {
    setMessage({ type, text });
    setTimeout(() => setMessage(null), 3000);
  };

  const loadMessages = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (filter.messageType) params.append('messageType', filter.messageType);
      if (filter.sendStatus) params.append('sendStatus', filter.sendStatus);
      if (filter.startTime) params.append('startTime', new Date(filter.startTime).toISOString());
      if (filter.endTime) params.append('endTime', new Date(filter.endTime).toISOString());

      const result = await api.get(`/api/messages?${params.toString()}`);
      if (result.code === 200) {
        setMessages(result.data.records || []);
      }
    } catch (error) {
      showMessage('error', '加载消息记录失败');
    } finally {
      setLoading(false);
    }
  };

  const loadTemplates = async () => {
    try {
      const [smsResult, emailResult] = await Promise.all([
        api.get('/api/sms-templates'),
        api.get('/api/email-templates'),
      ]);

      if (smsResult.code === 200) {
        setTemplates(smsResult.data);
      }
      if (emailResult.code === 200) {
        setEmailTemplates(emailResult.data);
      }
    } catch (error) {
      console.error('加载模板失败:', error);
    }
  };

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const result = await api.post('/api/messages/send', sendForm);
      if (result.code === 200) {
        showMessage('success', '消息发送成功');
        setSendForm({ messageType: 'SMS', templateCode: '', recipient: '', variables: {} });
        if (activeTab === 'send') {
          setActiveTab('records');
          loadMessages();
        }
      } else {
        showMessage('error', result.msg || '消息发送失败');
      }
    } catch (error) {
      showMessage('error', '消息发送失败，请稍后重试');
    } finally {
      setLoading(false);
    }
  };

  const handleBatchSend = async (e: React.FormEvent) => {
    e.preventDefault();

    const recipients = batchForm.recipients
      .split(/[,，\n]/)
      .map(r => r.trim())
      .filter(r => r.length > 0);

    if (recipients.length === 0) {
      showMessage('error', '请输入至少一个接收方');
      return;
    }

    setLoading(true);

    try {
      const result = await api.post('/api/messages/batch-send', {
        messageType: batchForm.messageType,
        templateCode: batchForm.templateCode,
        recipients,
        variables: batchForm.variables,
      });

      if (result.code === 200) {
        showMessage('success', `批量发送完成: ${result.msg}`);
        setBatchForm({ messageType: 'SMS', templateCode: '', recipients: '', variables: {} });
        setActiveTab('records');
        loadMessages();
      } else {
        showMessage('error', result.msg || '批量发送失败');
      }
    } catch (error) {
      showMessage('error', '批量发送失败，请稍后重试');
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = async (id: number) => {
    try {
      const result = await api.post(`/api/messages/${id}/retry`, {});
      if (result.code === 200) {
        showMessage('success', '重试成功');
        loadMessages();
      } else {
        showMessage('error', result.msg || '重试失败');
      }
    } catch (error) {
      showMessage('error', '重试失败，请稍后重试');
    }
  };

  const handleExport = () => {
    const params = new URLSearchParams();
    if (filter.messageType) params.append('messageType', filter.messageType);
    if (filter.sendStatus) params.append('sendStatus', filter.sendStatus);
    if (filter.startTime) params.append('startTime', new Date(filter.startTime).toISOString());
    if (filter.endTime) params.append('endTime', new Date(filter.endTime).toISOString());

    window.open(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/api/messages/export?${params.toString()}`, '_blank');
    showMessage('success', '导出已开始下载');
  };

  const addVariable = (form: 'send' | 'batch') => {
    const key = prompt('请输入变量名称:');
    if (key) {
      if (form === 'send') {
        setSendForm(prev => ({ ...prev, variables: { ...prev.variables, [key]: '' } }));
      } else {
        setBatchForm(prev => ({ ...prev, variables: { ...prev.variables, [key]: '' } }));
      }
    }
  };

  const updateVariable = (form: 'send' | 'batch', key: string, value: string) => {
    if (form === 'send') {
      setSendForm(prev => ({ ...prev, variables: { ...prev.variables, [key]: value } }));
    } else {
      setBatchForm(prev => ({ ...prev, variables: { ...prev.variables, [key]: value } }));
    }
  };

  const removeVariable = (form: 'send' | 'batch', key: string) => {
    if (form === 'send') {
      setSendForm(prev => {
        const newVars = { ...prev.variables };
        delete newVars[key];
        return { ...prev, variables: newVars };
      });
    } else {
      setBatchForm(prev => {
        const newVars = { ...prev.variables };
        delete newVars[key];
        return { ...prev, variables: newVars };
      });
    }
  };

  const currentTemplates = (form: 'send' | 'batch') => {
    const type = form === 'send' ? sendForm.messageType : batchForm.messageType;
    return type === 'Email' ? emailTemplates : templates;
  };

  return (
    <div>
      {/* 消息提示 */}
      {message && (
        <div className={`fixed top-4 right-4 px-6 py-3 rounded-lg glass-card z-50 ${
          message.type === 'success' ? 'border-green-500' : 'border-red-500'
        }`}>
          <span className={message.type === 'success' ? 'text-green-400' : 'text-red-400'}>
            {message.text}
          </span>
        </div>
      )}

      <div className="mb-6">
        <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
          消息管理
        </h1>
        <p className="text-slate-400 mt-1">发送消息和查看发送记录</p>
      </div>

      {/* 标签页 */}
      <div className="flex gap-2 mb-6">
        <button
          onClick={() => setActiveTab('send')}
          className={`px-6 py-2.5 rounded-lg font-medium transition-all ${
            activeTab === 'send' ? 'btn-primary' : 'btn-secondary'
          }`}
        >
          📤 单条发送
        </button>
        <button
          onClick={() => setActiveTab('batch')}
          className={`px-6 py-2.5 rounded-lg font-medium transition-all ${
            activeTab === 'batch' ? 'btn-primary' : 'btn-secondary'
          }`}
        >
          📦 批量发送
        </button>
        <button
          onClick={() => { setActiveTab('records'); loadMessages(); }}
          className={`px-6 py-2.5 rounded-lg font-medium transition-all ${
            activeTab === 'records' ? 'btn-primary' : 'btn-secondary'
          }`}
        >
          📋 消息记录
        </button>
      </div>

      {/* 单条发送 */}
      {activeTab === 'send' && (
        <div className="glass-card p-8 rounded-xl">
          <h2 className="text-xl font-semibold mb-6 text-slate-100">发送消息</h2>
          <form onSubmit={handleSend} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">消息类型</label>
                <select
                  value={sendForm.messageType}
                  onChange={(e) => setSendForm({ ...sendForm, messageType: e.target.value, templateCode: '' })}
                  className="w-full rounded-lg px-4 py-2.5"
                  required
                >
                  <option value="SMS">短信</option>
                  <option value="Email">邮件</option>
                  <option value="AppPush">APP推送</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">模板</label>
                <select
                  value={sendForm.templateCode}
                  onChange={(e) => setSendForm({ ...sendForm, templateCode: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                  required
                >
                  <option value="">请选择模板</option>
                  {currentTemplates('send').map(t => (
                    <option key={t.id} value={t.code}>{t.name} ({t.code})</option>
                  ))}
                </select>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium mb-2 text-slate-300">
                接收方 ({sendForm.messageType === 'SMS' ? '手机号' : sendForm.messageType === 'Email' ? '邮箱' : '设备ID'})
              </label>
              <input
                type="text"
                value={sendForm.recipient}
                onChange={(e) => setSendForm({ ...sendForm, recipient: e.target.value })}
                className="w-full rounded-lg px-4 py-2.5"
                placeholder={sendForm.messageType === 'SMS' ? '13800138000' : sendForm.messageType === 'Email' ? 'user@example.com' : 'device_token'}
                required
              />
            </div>
            <div>
              <div className="flex justify-between items-center mb-2">
                <label className="block text-sm font-medium text-slate-300">模板变量</label>
                <button type="button" onClick={() => addVariable('send')} className="text-sm text-indigo-400 hover:text-indigo-300">
                  + 添加变量
                </button>
              </div>
              {Object.entries(sendForm.variables).map(([key, value]) => (
                <div key={key} className="flex gap-2 mb-2">
                  <input type="text" value={key} disabled className="w-1/3 rounded-lg px-4 py-2" />
                  <input
                    type="text"
                    value={value}
                    onChange={(e) => updateVariable('send', key, e.target.value)}
                    className="flex-1 rounded-lg px-4 py-2"
                    placeholder="变量值"
                  />
                  <button type="button" onClick={() => removeVariable('send', key)} className="px-4 py-2 text-red-400 hover:text-red-300">
                    ✖
                  </button>
                </div>
              ))}
            </div>
            <button type="submit" disabled={loading} className="btn-primary px-8 py-3 rounded-lg font-medium w-full disabled:opacity-50">
              {loading ? '发送中...' : '📤 发送消息'}
            </button>
          </form>
        </div>
      )}

      {/* 批量发送 */}
      {activeTab === 'batch' && (
        <div className="glass-card p-8 rounded-xl">
          <h2 className="text-xl font-semibold mb-6 text-slate-100">批量发送消息</h2>
          <form onSubmit={handleBatchSend} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">消息类型</label>
                <select
                  value={batchForm.messageType}
                  onChange={(e) => setBatchForm({ ...batchForm, messageType: e.target.value, templateCode: '' })}
                  className="w-full rounded-lg px-4 py-2.5"
                  required
                >
                  <option value="SMS">短信</option>
                  <option value="Email">邮件</option>
                  <option value="AppPush">APP推送</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">模板</label>
                <select
                  value={batchForm.templateCode}
                  onChange={(e) => setBatchForm({ ...batchForm, templateCode: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                  required
                >
                  <option value="">请选择模板</option>
                  {currentTemplates('batch').map(t => (
                    <option key={t.id} value={t.code}>{t.name} ({t.code})</option>
                  ))}
                </select>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium mb-2 text-slate-300">
                接收方列表 (每行一个或用逗号分隔)
              </label>
              <textarea
                value={batchForm.recipients}
                onChange={(e) => setBatchForm({ ...batchForm, recipients: e.target.value })}
                className="w-full rounded-lg px-4 py-2.5 font-mono text-sm"
                rows={6}
                placeholder="13800138000&#10;13800138001&#10;13800138002"
                required
              />
              <p className="text-xs text-slate-500 mt-1">
                总计: {batchForm.recipients.split(/[,，\n]/).filter(r => r.trim()).length} 个接收方
              </p>
            </div>
            <div>
              <div className="flex justify-between items-center mb-2">
                <label className="block text-sm font-medium text-slate-300">模板变量 (统一使用)</label>
                <button type="button" onClick={() => addVariable('batch')} className="text-sm text-indigo-400 hover:text-indigo-300">
                  + 添加变量
                </button>
              </div>
              {Object.entries(batchForm.variables).map(([key, value]) => (
                <div key={key} className="flex gap-2 mb-2">
                  <input type="text" value={key} disabled className="w-1/3 rounded-lg px-4 py-2" />
                  <input
                    type="text"
                    value={value}
                    onChange={(e) => updateVariable('batch', key, e.target.value)}
                    className="flex-1 rounded-lg px-4 py-2"
                    placeholder="变量值"
                  />
                  <button type="button" onClick={() => removeVariable('batch', key)} className="px-4 py-2 text-red-400 hover:text-red-300">
                    ✖
                  </button>
                </div>
              ))}
            </div>
            <button type="submit" disabled={loading} className="btn-primary px-8 py-3 rounded-lg font-medium w-full disabled:opacity-50">
              {loading ? '发送中...' : '📦 批量发送'}
            </button>
          </form>
        </div>
      )}

      {/* 消息记录 */}
      {activeTab === 'records' && (
        <div>
          {/* 筛选条件 */}
          <div className="glass-card p-6 rounded-xl mb-6">
            <div className="grid grid-cols-4 gap-4">
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">消息类型</label>
                <select
                  value={filter.messageType}
                  onChange={(e) => setFilter({ ...filter, messageType: e.target.value })}
                  className="w-full rounded-lg px-4 py-2"
                >
                  <option value="">全部</option>
                  <option value="SMS">短信</option>
                  <option value="Email">邮件</option>
                  <option value="AppPush">APP推送</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">发送状态</label>
                <select
                  value={filter.sendStatus}
                  onChange={(e) => setFilter({ ...filter, sendStatus: e.target.value })}
                  className="w-full rounded-lg px-4 py-2"
                >
                  <option value="">全部</option>
                  <option value="待发送">待发送</option>
                  <option value="发送中">发送中</option>
                  <option value="成功">成功</option>
                  <option value="失败">失败</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">开始时间</label>
                <input
                  type="datetime-local"
                  value={filter.startTime}
                  onChange={(e) => setFilter({ ...filter, startTime: e.target.value })}
                  className="w-full rounded-lg px-4 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">结束时间</label>
                <input
                  type="datetime-local"
                  value={filter.endTime}
                  onChange={(e) => setFilter({ ...filter, endTime: e.target.value })}
                  className="w-full rounded-lg px-4 py-2"
                />
              </div>
            </div>
            <div className="flex gap-3 mt-4">
              <button onClick={loadMessages} className="btn-primary px-6 py-2 rounded-lg">
                🔍 查询
              </button>
              <button onClick={handleExport} className="btn-secondary px-6 py-2 rounded-lg">
                📥 导出CSV
              </button>
            </div>
          </div>

          {/* 消息列表 */}
          <div className="glass-card rounded-xl overflow-hidden">
            {loading ? (
              <div className="px-6 py-12 text-center text-slate-400">加载中...</div>
            ) : (
              <table className="min-w-full">
                <thead>
                  <tr className="border-b border-slate-700">
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">任务ID</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">类型</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">模板</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">接收方</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">厂商</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">状态</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">时间</th>
                    <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {messages.length === 0 ? (
                    <tr>
                      <td colSpan={8} className="px-6 py-12 text-center text-slate-400">
                        暂无消息记录
                      </td>
                    </tr>
                  ) : (
                    messages.map((msg) => (
                      <tr key={msg.id} className="border-b border-slate-800 hover:bg-slate-800/30 transition-colors">
                        <td className="px-6 py-4">
                          <code className="px-2 py-1 bg-slate-700/50 rounded text-xs text-indigo-300">
                            {msg.taskId.substring(0, 8)}...
                          </code>
                        </td>
                        <td className="px-6 py-4 text-sm text-slate-300">{msg.messageType}</td>
                        <td className="px-6 py-4 text-sm text-slate-300">{msg.templateCode}</td>
                        <td className="px-6 py-4 text-sm text-slate-300">{msg.recipient}</td>
                        <td className="px-6 py-4 text-sm text-slate-300">{msg.manufacturer?.name || '-'}</td>
                        <td className="px-6 py-4">
                          <span className={`badge ${
                            msg.sendStatus === '成功' ? 'badge-success' :
                            msg.sendStatus === '失败' ? 'badge-error' : 'badge-warning'
                          }`}>
                            {msg.sendStatus}
                          </span>
                          {msg.failureReason && (
                            <div className="text-xs text-red-400 mt-1">{msg.failureReason}</div>
                          )}
                        </td>
                        <td className="px-6 py-4 text-sm text-slate-400">
                          {new Date(msg.createdAt).toLocaleString('zh-CN')}
                        </td>
                        <td className="px-6 py-4">
                          {msg.sendStatus === '失败' && (
                            <button
                              onClick={() => handleRetry(msg.id)}
                              className="text-indigo-400 hover:text-indigo-300 font-medium text-sm transition-colors"
                            >
                              🔄 重试
                            </button>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
