'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

interface SmsTemplate {
  id: number;
  name: string;
  code: string;
  content: string;
  variables?: string;
  auditStatus?: string;
  isActive: boolean;
  manufacturerId: number;
  manufacturer?: { id: number; name: string };
  createdAt: string;
}

interface Manufacturer {
  id: number;
  name: string;
}

export default function SmsTemplatesPage() {
  const [templates, setTemplates] = useState<SmsTemplate[]>([]);
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<SmsTemplate | null>(null);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    code: '',
    content: '',
    variables: '',
    auditStatus: '未审核',
    isActive: true,
    manufacturerId: 0,
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [templatesResult, manufacturersResult] = await Promise.all([
        api.get('/api/sms-templates'),
        api.get('/api/manufacturers'),
      ]);

      if (templatesResult.code === 200) {
        setTemplates(templatesResult.data);
      }
      if (manufacturersResult.code === 200) {
        setManufacturers(manufacturersResult.data.filter((m: any) => m.supportedChannels.includes('SMS') && m.isActive));
      }
    } catch (error) {
      showMessage('error', '加载数据失败');
    } finally {
      setLoading(false);
    }
  };

  const showMessage = (type: 'success' | 'error', text: string) => {
    setMessage({ type, text });
    setTimeout(() => setMessage(null), 3000);
  };

  const handleCreate = () => {
    setEditing(null);
    setFormData({
      name: '',
      code: '',
      content: '',
      variables: '',
      auditStatus: '未审核',
      isActive: true,
      manufacturerId: manufacturers[0]?.id || 0,
    });
    setShowModal(true);
  };

  const handleEdit = (template: SmsTemplate) => {
    setEditing(template);
    setFormData({
      name: template.name,
      code: template.code,
      content: template.content,
      variables: template.variables || '',
      auditStatus: template.auditStatus || '未审核',
      isActive: template.isActive,
      manufacturerId: template.manufacturerId,
    });
    setShowModal(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const result = editing
        ? await api.put(`/api/sms-templates/${editing.id}`, formData)
        : await api.post('/api/sms-templates', formData);

      if (result.code === 200) {
        showMessage('success', editing ? '模板更新成功' : '模板创建成功');
        setShowModal(false);
        loadData();
      } else {
        showMessage('error', result.msg || '操作失败');
      }
    } catch (error) {
      showMessage('error', '操作失败，请稍后重试');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: number, name: string) => {
    if (!confirm(`确定要删除模板"${name}"吗？`)) return;

    try {
      const result = await api.delete(`/api/sms-templates/${id}`);
      if (result.code === 200) {
        showMessage('success', '模板删除成功');
        loadData();
      } else {
        showMessage('error', result.msg || '删除失败');
      }
    } catch (error) {
      showMessage('error', '删除失败，请稍后重试');
    }
  };

  if (loading && templates.length === 0) {
    return <div className="flex items-center justify-center h-64">
      <div className="text-slate-400">加载中...</div>
    </div>;
  }

  return (
    <div>
      {message && (
        <div className={`fixed top-4 right-4 px-6 py-3 rounded-lg glass-card z-50 ${
          message.type === 'success' ? 'border-green-500' : 'border-red-500'
        }`}>
          <span className={message.type === 'success' ? 'text-green-400' : 'text-red-400'}>
            {message.text}
          </span>
        </div>
      )}

      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
            短信模板管理
          </h1>
          <p className="text-slate-400 mt-1">管理短信模板配置和变量</p>
        </div>
        <button onClick={handleCreate} className="btn-primary px-6 py-2.5 rounded-lg font-medium">
          ➕ 新增模板
        </button>
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 flex items-center justify-center">
          <div className="glass-card p-8 rounded-xl max-w-2xl w-full mx-4">
            <h2 className="text-2xl font-semibold mb-6 text-slate-100">
              {editing ? '编辑模板' : '新增模板'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2 text-slate-300">模板名称</label>
                  <input
                    type="text"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full rounded-lg px-4 py-2.5"
                    required
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-2 text-slate-300">模板编码</label>
                  <input
                    type="text"
                    value={formData.code}
                    onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                    className="w-full rounded-lg px-4 py-2.5"
                    required
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">所属厂商</label>
                <select
                  value={formData.manufacturerId}
                  onChange={(e) => setFormData({ ...formData, manufacturerId: parseInt(e.target.value) })}
                  className="w-full rounded-lg px-4 py-2.5"
                  required
                >
                  {manufacturers.map(m => (
                    <option key={m.id} value={m.id}>{m.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">模板内容</label>
                <textarea
                  value={formData.content}
                  onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5 font-mono text-sm"
                  rows={4}
                  placeholder="您的验证码是{code}，有效期{minutes}分钟"
                  required
                />
                <p className="text-xs text-slate-500 mt-1">使用{'{'}变量名{'}'} 格式定义变量</p>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">变量说明(可选)</label>
                <input
                  type="text"
                  value={formData.variables}
                  onChange={(e) => setFormData({ ...formData, variables: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                  placeholder="code:验证码,minutes:有效期分钟数"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2 text-slate-300">审核状态</label>
                  <select
                    value={formData.auditStatus}
                    onChange={(e) => setFormData({ ...formData, auditStatus: e.target.value })}
                    className="w-full rounded-lg px-4 py-2.5"
                  >
                    <option value="未审核">未审核</option>
                    <option value="已审核">已审核</option>
                    <option value="已通过">已通过</option>
                    <option value="已拒绝">已拒绝</option>
                  </select>
                </div>
                <div className="flex items-center">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="w-4 h-4 rounded"
                  />
                  <label htmlFor="isActive" className="ml-2 text-sm text-slate-300">启用模板</label>
                </div>
              </div>
              <div className="flex gap-3 mt-6">
                <button type="submit" disabled={loading} className="btn-primary px-6 py-2.5 rounded-lg font-medium disabled:opacity-50">
                  {loading ? '保存中...' : '💾 保存'}
                </button>
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary px-6 py-2.5 rounded-lg font-medium">
                  ✖️ 取消
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="glass-card rounded-xl overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr className="border-b border-slate-700">
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">模板名称</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">编码</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">厂商</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">内容</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">审核状态</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">状态</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase">操作</th>
            </tr>
          </thead>
          <tbody>
            {templates.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-6 py-12 text-center text-slate-400">
                  暂无模板数据，点击右上角"新增模板"开始创建
                </td>
              </tr>
            ) : (
              templates.map((template) => (
                <tr key={template.id} className="border-b border-slate-800 hover:bg-slate-800/30 transition-colors">
                  <td className="px-6 py-4 text-slate-300">{template.name}</td>
                  <td className="px-6 py-4">
                    <code className="px-2 py-1 bg-slate-700/50 rounded text-xs text-indigo-300">
                      {template.code}
                    </code>
                  </td>
                  <td className="px-6 py-4 text-sm text-slate-300">{template.manufacturer?.name || '-'}</td>
                  <td className="px-6 py-4 text-sm text-slate-400 max-w-xs truncate">{template.content}</td>
                  <td className="px-6 py-4">
                    <span className={`badge ${
                      template.auditStatus === '已审核' || template.auditStatus === '已通过' ? 'badge-success' :
                      template.auditStatus === '已拒绝' ? 'badge-error' : 'badge-warning'
                    }`}>
                      {template.auditStatus || '未审核'}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`badge ${template.isActive ? 'badge-success' : 'badge-error'}`}>
                      {template.isActive ? '启用' : '禁用'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <button
                      onClick={() => handleEdit(template)}
                      className="text-indigo-400 hover:text-indigo-300 font-medium mr-3 transition-colors"
                    >
                      ✏️ 编辑
                    </button>
                    <button
                      onClick={() => handleDelete(template.id, template.name)}
                      className="text-red-400 hover:text-red-300 font-medium transition-colors"
                    >
                      🗑️ 删除
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
