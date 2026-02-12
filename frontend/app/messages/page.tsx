'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

export default function MessagesPage() {
  const [messages, setMessages] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadMessages();
  }, []);

  const loadMessages = async () => {
    try {
      const result = await api.get('/api/messages');
      if (result.code === 200) {
        setMessages(result.data.records);
      }
    } catch (error) {
      console.error('加载消息记录失败:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = async (id: number) => {
    try {
      await api.post(`/api/messages/${id}/retry`, {});
      loadMessages();
    } catch (error) {
      console.error('重试失败:', error);
    }
  };

  if (loading) {
    return <div className="flex items-center justify-center h-64">
      <div className="text-slate-400">加载中...</div>
    </div>;
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
          消息记录
        </h1>
        <p className="text-slate-400 mt-1">查看和管理消息发送记录</p>
      </div>

      <div className="glass-card rounded-xl overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">任务ID</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">类型</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">模板</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">接收方</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">状态</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">操作</th>
            </tr>
          </thead>
          <tbody>
            {messages.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-12 text-center text-slate-400">
                  暂无消息记录
                </td>
              </tr>
            ) : (
              messages.map((message) => (
                <tr key={message.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <code className="px-2 py-1 bg-slate-700/50 rounded text-xs text-indigo-300">
                      {message.taskId}
                    </code>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-slate-300">{message.messageType}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-slate-300">{message.templateCode}</td>
                  <td className="px-6 py-4 whitespace-nowrap text-slate-300">{message.recipient}</td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`badge ${
                      message.sendStatus === '成功' ? 'badge-success' :
                      message.sendStatus === '失败' ? 'badge-error' : 'badge-warning'
                    }`}>
                      {message.sendStatus}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {message.sendStatus === '失败' && (
                      <button
                        onClick={() => handleRetry(message.id)}
                        className="text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
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
      </div>
    </div>
  );
}
