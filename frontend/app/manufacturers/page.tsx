'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

interface Manufacturer {
  id: number;
  name: string;
  code: string;
  description?: string;
  supportedChannels: string;
  smsConfig?: string;
  emailConfig?: string;
  appPushConfig?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export default function ManufacturersPage() {
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    description: '',
    supportedChannels: 'SMS',
    smsConfig: '',
    emailConfig: '',
    appPushConfig: '',
    isActive: true,
  });

  useEffect(() => {
    loadManufacturers();
  }, []);

  const loadManufacturers = async () => {
    try {
      const result = await api.get('/api/manufacturers');
      if (result.code === 200) {
        setManufacturers(result.data);
      }
    } catch (error) {
      showMessage('error', '加载厂商列表失败');
    } finally {
      setLoading(false);
    }
  };

  const showMessage = (type: 'success' | 'error', text: string) => {
    setMessage({ type, text });
    setTimeout(() => setMessage(null), 3000);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      if (editingId) {
        const result = await api.put(`/api/manufacturers/${editingId}`, formData);
        if (result.code === 200) {
          showMessage('success', '厂商更新成功');
        } else {
          showMessage('error', result.msg || '更新失败');
        }
      } else {
        const result = await api.post('/api/manufacturers', formData);
        if (result.code === 200) {
          showMessage('success', '厂商创建成功');
        } else {
          showMessage('error', result.msg || '创建失败');
        }
      }
      setShowForm(false);
      setEditingId(null);
      resetForm();
      loadManufacturers();
    } catch (error) {
      showMessage('error', '操作失败，请稍后重试');
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (manufacturer: Manufacturer) => {
    setFormData({
      name: manufacturer.name,
      code: manufacturer.code,
      description: manufacturer.description || '',
      supportedChannels: manufacturer.supportedChannels,
      smsConfig: manufacturer.smsConfig || '',
      emailConfig: manufacturer.emailConfig || '',
      appPushConfig: manufacturer.appPushConfig || '',
      isActive: manufacturer.isActive,
    });
    setEditingId(manufacturer.id);
    setShowForm(true);
  };

  const handleDelete = async (id: number, name: string) => {
    if (confirm(`确定要删除厂商"${name}"吗？`)) {
      try {
        const result = await api.delete(`/api/manufacturers/${id}`);
        if (result.code === 200) {
          showMessage('success', '厂商删除成功');
          loadManufacturers();
        } else {
          showMessage('error', result.msg || '删除失败');
        }
      } catch (error) {
        showMessage('error', '删除失败，请稍后重试');
      }
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      code: '',
      description: '',
      supportedChannels: 'SMS',
      smsConfig: '',
      emailConfig: '',
      appPushConfig: '',
      isActive: true,
    });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-400">加载中...</div>
      </div>
    );
  }

  return (
    <div>
      {/* 消息提示 */}
      {message && (
        <div className={`fixed top-4 right-4 px-6 py-3 rounded-lg glass-card z-50 animate-fade-in ${
          message.type === 'success' ? 'border-green-500' : 'border-red-500'
        }`}>
          <span className={message.type === 'success' ? 'text-green-400' : 'text-red-400'}>
            {message.text}
          </span>
        </div>
      )}

      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
            厂商管理
          </h1>
          <p className="text-slate-400 mt-1">管理消息发送厂商和渠道配置</p>
        </div>
        <button
          onClick={() => {
            setShowForm(true);
            setEditingId(null);
            resetForm();
          }}
          className="btn-primary px-6 py-2.5 rounded-lg font-medium"
        >
          ✨ 新增厂商
        </button>
      </div>

      {/* 表单弹窗 */}
      {showForm && (
        <div className="glass-card p-6 rounded-xl mb-6 border-indigo-500/30">
          <h2 className="text-xl font-semibold mb-6 text-slate-100">
            {editingId ? '编辑厂商' : '新增厂商'}
          </h2>
          <form onSubmit={handleSubmit}>
            <div className="grid grid-cols-2 gap-4 mb-6">
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">厂商名称</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                  placeholder="请输入厂商名称"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">厂商编码</label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                  placeholder="请输入唯一编码"
                  required
                />
              </div>
              <div className="col-span-2">
                <label className="block text-sm font-medium mb-2 text-slate-300">厂商描述</label>
                <input
                  type="text"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                  placeholder="请输入厂商描述（可选）"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">支持渠道</label>
                <select
                  value={formData.supportedChannels}
                  onChange={(e) => setFormData({ ...formData, supportedChannels: e.target.value })}
                  className="w-full rounded-lg px-4 py-2.5"
                >
                  <option value="SMS">短信</option>
                  <option value="Email">邮件</option>
                  <option value="AppPush">APP推送</option>
                  <option value="SMS,Email">短信+邮件</option>
                  <option value="SMS,Email,AppPush">全部</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-2 text-slate-300">状态</label>
                <select
                  value={formData.isActive.toString()}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.value === 'true' })}
                  className="w-full rounded-lg px-4 py-2.5"
                >
                  <option value="true">启用</option>
                  <option value="false">禁用</option>
                </select>
              </div>
            </div>
            <div className="flex gap-3">
              <button
                type="submit"
                disabled={submitting}
                className="btn-primary px-6 py-2.5 rounded-lg font-medium disabled:opacity-50"
              >
                {submitting ? '保存中...' : '💾 保存'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowForm(false);
                  setEditingId(null);
                  resetForm();
                }}
                className="btn-secondary px-6 py-2.5 rounded-lg font-medium"
              >
                ✖️ 取消
              </button>
            </div>
          </form>
        </div>
      )}

      {/* 表格 */}
      <div className="glass-card rounded-xl overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">厂商名称</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">编码</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">支持渠道</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">状态</th>
              <th className="px-6 py-4 text-left text-xs font-semibold text-slate-300 uppercase tracking-wider">操作</th>
            </tr>
          </thead>
          <tbody>
            {manufacturers.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-6 py-12 text-center text-slate-400">
                  暂无数据，请点击上方按钮新增厂商
                </td>
              </tr>
            ) : (
              manufacturers.map((manufacturer) => (
                <tr key={manufacturer.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-slate-100 font-medium">{manufacturer.name}</div>
                    {manufacturer.description && (
                      <div className="text-xs text-slate-400 mt-0.5">{manufacturer.description}</div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <code className="px-2 py-1 bg-slate-700/50 rounded text-sm text-indigo-300">
                      {manufacturer.code}
                    </code>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-slate-300">
                    {manufacturer.supportedChannels}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`badge ${manufacturer.isActive ? 'badge-success' : 'badge-error'}`}>
                      {manufacturer.isActive ? '已启用' : '已禁用'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap space-x-3">
                    <button
                      onClick={() => handleEdit(manufacturer)}
                      className="text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
                    >
                      ✏️ 编辑
                    </button>
                    <button
                      onClick={() => handleDelete(manufacturer.id, manufacturer.name)}
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
