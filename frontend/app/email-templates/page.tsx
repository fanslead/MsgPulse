'use client';

import { useState, useEffect } from 'react';
import { useConfirm } from '@/components/ConfirmDialog';

interface EmailTemplate {
  id: number;
  name: string;
  code: string;
  subject: string;
  content: string;
  contentType: string;
  variables?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

interface Message {
  type: 'success' | 'error';
  text: string;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

const api = {
  async get(url: string) {
    const res = await fetch(`${API_URL}${url}`);
    return res.json();
  },
  async post(url: string, data: any) {
    const res = await fetch(`${API_URL}${url}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return res.json();
  },
  async put(url: string, data: any) {
    const res = await fetch(`${API_URL}${url}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return res.json();
  },
  async delete(url: string) {
    const res = await fetch(`${API_URL}${url}`, {
      method: 'DELETE',
    });
    return res.json();
  },
};

export default function EmailTemplatesPage() {
  const [templates, setTemplates] = useState<EmailTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<EmailTemplate | null>(null);
  const [message, setMessage] = useState<Message | null>(null);
  const { confirm } = useConfirm();
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    subject: '',
    content: '',
    contentType: 'Text',
    variables: '',
    isActive: true,
  });

  const showMessage = (type: 'success' | 'error', text: string) => {
    setMessage({ type, text });
    setTimeout(() => setMessage(null), 3000);
  };

  const loadData = async () => {
    setLoading(true);
    const result = await api.get('/api/email-templates');
    if (result.code === 200) {
      setTemplates(result.data);
    } else {
      showMessage('error', result.msg || '加载失败');
    }
    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCreate = () => {
    setEditing(null);
    setFormData({
      name: '',
      code: '',
      subject: '',
      content: '',
      contentType: 'Text',
      variables: '',
      isActive: true,
    });
    setShowModal(true);
  };

  const handleEdit = (template: EmailTemplate) => {
    setEditing(template);
    setFormData({
      name: template.name,
      code: template.code,
      subject: template.subject,
      content: template.content,
      contentType: template.contentType,
      variables: template.variables || '',
      isActive: template.isActive,
    });
    setShowModal(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const result = editing
      ? await api.put(`/api/email-templates/${editing.id}`, formData)
      : await api.post('/api/email-templates', formData);

    if (result.code === 200) {
      showMessage('success', editing ? '模板更新成功' : '模板创建成功');
      setShowModal(false);
      loadData();
    } else {
      showMessage('error', result.msg || '操作失败');
    }
  };

  const handleDelete = async (id: number) => {
    const confirmed = await confirm({
      title: '确认删除',
      message: '确认删除此邮件模板吗？此操作不可撤销。',
      confirmText: '删除',
      cancelText: '取消'
    });

    if (!confirmed) return;

    const result = await api.delete(`/api/email-templates/${id}`);
    if (result.code === 200) {
      showMessage('success', '删除成功');
      loadData();
    } else {
      showMessage('error', result.msg || '删除失败');
    }
  };

  const handleToggleActive = async (template: EmailTemplate) => {
    const result = await api.put(`/api/email-templates/${template.id}`, {
      ...template,
      isActive: !template.isActive,
    });

    if (result.code === 200) {
      showMessage('success', template.isActive ? '已禁用' : '已启用');
      loadData();
    } else {
      showMessage('error', result.msg || '操作失败');
    }
  };

  return (
    <div>
      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
            邮件模板
          </h1>
          <p className="text-slate-400 mt-1">管理邮件模板配置</p>
        </div>
        <button
          onClick={handleCreate}
          className="px-6 py-2.5 bg-gradient-to-r from-indigo-500 to-purple-500 text-white rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all shadow-lg hover:shadow-indigo-500/50"
        >
          + 新建模板
        </button>
      </div>

      {message && (
        <div
          className={`mb-4 p-4 rounded-lg ${
            message.type === 'success'
              ? 'bg-green-500/20 text-green-300 border border-green-500/50'
              : 'bg-red-500/20 text-red-300 border border-red-500/50'
          }`}
        >
          {message.text}
        </div>
      )}

      <div className="glass-card rounded-xl overflow-hidden">
        {loading ? (
          <div className="p-12 text-center text-slate-400">加载中...</div>
        ) : templates.length === 0 ? (
          <div className="p-12 text-center">
            <div className="text-6xl mb-4">✉️</div>
            <p className="text-slate-300 text-lg">暂无邮件模板</p>
            <p className="text-slate-400 text-sm mt-2">点击右上角按钮创建第一个模板</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-800/50 border-b border-slate-700/50">
                <tr>
                  <th className="text-left p-4 text-slate-300 font-medium">模板名称</th>
                  <th className="text-left p-4 text-slate-300 font-medium">模板编码</th>
                  <th className="text-left p-4 text-slate-300 font-medium">主题</th>
                  <th className="text-left p-4 text-slate-300 font-medium">内容类型</th>
                  <th className="text-left p-4 text-slate-300 font-medium">状态</th>
                  <th className="text-left p-4 text-slate-300 font-medium">更新时间</th>
                  <th className="text-right p-4 text-slate-300 font-medium">操作</th>
                </tr>
              </thead>
              <tbody>
                {templates.map((template) => (
                  <tr
                    key={template.id}
                    className="border-b border-slate-700/30 hover:bg-slate-800/30 transition-colors"
                  >
                    <td className="p-4 text-slate-200">{template.name}</td>
                    <td className="p-4">
                      <code className="text-indigo-400 bg-slate-800/50 px-2 py-1 rounded text-sm">
                        {template.code}
                      </code>
                    </td>
                    <td className="p-4 text-slate-300">{template.subject}</td>
                    <td className="p-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          template.contentType === 'HTML'
                            ? 'bg-purple-500/20 text-purple-300'
                            : 'bg-slate-500/20 text-slate-300'
                        }`}
                      >
                        {template.contentType}
                      </span>
                    </td>
                    <td className="p-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          template.isActive
                            ? 'bg-green-500/20 text-green-300'
                            : 'bg-slate-500/20 text-slate-400'
                        }`}
                      >
                        {template.isActive ? '启用' : '禁用'}
                      </span>
                    </td>
                    <td className="p-4 text-slate-400 text-sm">
                      {new Date(template.updatedAt).toLocaleString('zh-CN')}
                    </td>
                    <td className="p-4 text-right space-x-2">
                      <button
                        onClick={() => handleEdit(template)}
                        className="text-indigo-400 hover:text-indigo-300"
                      >
                        编辑
                      </button>
                      <button
                        onClick={() => handleToggleActive(template)}
                        className="text-yellow-400 hover:text-yellow-300"
                      >
                        {template.isActive ? '禁用' : '启用'}
                      </button>
                      <button
                        onClick={() => handleDelete(template.id)}
                        className="text-red-400 hover:text-red-300"
                      >
                        删除
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="glass-card rounded-xl p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <h2 className="text-2xl font-bold text-slate-200 mb-6">
              {editing ? '编辑模板' : '新建模板'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-slate-300 mb-2">模板名称 *</label>
                  <input
                    type="text"
                    required
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                    placeholder="例: 欢迎邮件"
                  />
                </div>
                <div>
                  <label className="block text-slate-300 mb-2">模板编码 *</label>
                  <input
                    type="text"
                    required
                    value={formData.code}
                    onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                    className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                    placeholder="例: WELCOME_EMAIL"
                  />
                </div>
              </div>

              <div>
                <label className="block text-slate-300 mb-2">邮件主题 *</label>
                <input
                  type="text"
                  required
                  value={formData.subject}
                  onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                  placeholder="例: 欢迎加入{companyName}"
                />
              </div>

              <div>
                <label className="block text-slate-300 mb-2">内容类型</label>
                <select
                  value={formData.contentType}
                  onChange={(e) => setFormData({ ...formData, contentType: e.target.value })}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                >
                  <option value="Text">纯文本</option>
                  <option value="HTML">HTML</option>
                </select>
              </div>

              <div>
                <label className="block text-slate-300 mb-2">邮件内容 *</label>
                <textarea
                  required
                  value={formData.content}
                  onChange={(e) => setFormData({ ...formData, content: e.target.value })}
                  rows={8}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500 font-mono text-sm"
                  placeholder={
                    formData.contentType === 'HTML'
                      ? '<p>尊敬的{userName}:</p>\n<p>欢迎加入{companyName}!</p>'
                      : '尊敬的{userName}:\n欢迎加入{companyName}!'
                  }
                />
              </div>

              <div>
                <label className="block text-slate-300 mb-2">变量说明</label>
                <input
                  type="text"
                  value={formData.variables}
                  onChange={(e) => setFormData({ ...formData, variables: e.target.value })}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                  placeholder="例: userName=用户名,companyName=公司名称"
                />
                <p className="text-slate-500 text-xs mt-1">
                  用于说明模板中的变量含义，格式: 变量名=说明,变量名=说明
                </p>
              </div>

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="mr-2"
                />
                <label htmlFor="isActive" className="text-slate-300">
                  启用此模板
                </label>
              </div>

              <div className="flex justify-end space-x-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-6 py-2 bg-slate-700/50 text-slate-300 rounded-lg hover:bg-slate-700 transition-colors"
                >
                  取消
                </button>
                <button
                  type="submit"
                  className="px-6 py-2 bg-gradient-to-r from-indigo-500 to-purple-500 text-white rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all"
                >
                  {editing ? '保存' : '创建'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
