'use client';

import { useState, useEffect } from 'react';

interface RouteRule {
  id: number;
  name: string;
  messageType: string;
  manufacturerId: number;
  manufacturer?: {
    id: number;
    name: string;
    providerType: string;
  };
  priority: number;
  matchConditions?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

interface Manufacturer {
  id: number;
  name: string;
  providerType: string;
  supportedChannels: string[];
  isActive: boolean;
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

const channelMap: Record<string, string> = {
  SMS: '短信',
  Email: '邮件',
  AppPush: 'APP推送',
};

export default function RouteRulesPage() {
  const [rules, setRules] = useState<RouteRule[]>([]);
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<RouteRule | null>(null);
  const [message, setMessage] = useState<Message | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    messageType: 'SMS',
    manufacturerId: 0,
    priority: 1,
    matchConditions: '',
    isActive: true,
  });

  const showMessage = (type: 'success' | 'error', text: string) => {
    setMessage({ type, text });
    setTimeout(() => setMessage(null), 3000);
  };

  const loadData = async () => {
    setLoading(true);
    const [rulesResult, manufacturersResult] = await Promise.all([
      api.get('/api/route-rules'),
      api.get('/api/manufacturers'),
    ]);

    if (rulesResult.code === 200) {
      setRules(rulesResult.data);
    } else {
      showMessage('error', rulesResult.msg || '加载规则失败');
    }

    if (manufacturersResult.code === 200) {
      setManufacturers(manufacturersResult.data.filter((m: Manufacturer) => m.isActive));
    } else {
      showMessage('error', manufacturersResult.msg || '加载厂商失败');
    }

    setLoading(false);
  };

  useEffect(() => {
    loadData();
  }, []);

  const getFilteredManufacturers = () => {
    return manufacturers.filter((m) => m.supportedChannels.includes(formData.messageType));
  };

  const handleCreate = () => {
    setEditing(null);
    setFormData({
      name: '',
      messageType: 'SMS',
      manufacturerId: 0,
      priority: 1,
      matchConditions: '',
      isActive: true,
    });
    setShowModal(true);
  };

  const handleEdit = (rule: RouteRule) => {
    setEditing(rule);
    setFormData({
      name: rule.name,
      messageType: rule.messageType,
      manufacturerId: rule.manufacturerId,
      priority: rule.priority,
      matchConditions: rule.matchConditions || '',
      isActive: rule.isActive,
    });
    setShowModal(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate match conditions JSON if provided
    if (formData.matchConditions && formData.matchConditions.trim()) {
      try {
        JSON.parse(formData.matchConditions);
      } catch {
        showMessage('error', '匹配条件必须是有效的JSON格式');
        return;
      }
    }

    const result = editing
      ? await api.put(`/api/route-rules/${editing.id}`, formData)
      : await api.post('/api/route-rules', formData);

    if (result.code === 200) {
      showMessage('success', editing ? '规则更新成功' : '规则创建成功');
      setShowModal(false);
      loadData();
    } else {
      showMessage('error', result.msg || '操作失败');
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('确认删除此规则?')) return;

    const result = await api.delete(`/api/route-rules/${id}`);
    if (result.code === 200) {
      showMessage('success', '删除成功');
      loadData();
    } else {
      showMessage('error', result.msg || '删除失败');
    }
  };

  const handleToggleActive = async (rule: RouteRule) => {
    const result = await api.put(`/api/route-rules/${rule.id}`, {
      ...rule,
      isActive: !rule.isActive,
    });

    if (result.code === 200) {
      showMessage('success', rule.isActive ? '已禁用' : '已启用');
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
            路由规则
          </h1>
          <p className="text-slate-400 mt-1">配置消息路由规则</p>
        </div>
        <button
          onClick={handleCreate}
          className="px-6 py-2.5 bg-gradient-to-r from-indigo-500 to-purple-500 text-white rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all shadow-lg hover:shadow-indigo-500/50"
        >
          + 新建规则
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
        ) : rules.length === 0 ? (
          <div className="p-12 text-center">
            <div className="text-6xl mb-4">🔀</div>
            <p className="text-slate-300 text-lg">暂无路由规则</p>
            <p className="text-slate-400 text-sm mt-2">点击右上角按钮创建第一条规则</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-800/50 border-b border-slate-700/50">
                <tr>
                  <th className="text-left p-4 text-slate-300 font-medium">规则名称</th>
                  <th className="text-left p-4 text-slate-300 font-medium">消息类型</th>
                  <th className="text-left p-4 text-slate-300 font-medium">目标厂商</th>
                  <th className="text-left p-4 text-slate-300 font-medium">优先级</th>
                  <th className="text-left p-4 text-slate-300 font-medium">状态</th>
                  <th className="text-left p-4 text-slate-300 font-medium">更新时间</th>
                  <th className="text-right p-4 text-slate-300 font-medium">操作</th>
                </tr>
              </thead>
              <tbody>
                {rules.map((rule) => (
                  <tr
                    key={rule.id}
                    className="border-b border-slate-700/30 hover:bg-slate-800/30 transition-colors"
                  >
                    <td className="p-4 text-slate-200">{rule.name}</td>
                    <td className="p-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          rule.messageType === 'SMS'
                            ? 'bg-blue-500/20 text-blue-300'
                            : rule.messageType === 'Email'
                            ? 'bg-purple-500/20 text-purple-300'
                            : 'bg-green-500/20 text-green-300'
                        }`}
                      >
                        {channelMap[rule.messageType] || rule.messageType}
                      </span>
                    </td>
                    <td className="p-4 text-slate-300">
                      {rule.manufacturer?.name || '未知厂商'}
                    </td>
                    <td className="p-4">
                      <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-indigo-500/20 text-indigo-300 text-sm font-medium">
                        {rule.priority}
                      </span>
                    </td>
                    <td className="p-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          rule.isActive
                            ? 'bg-green-500/20 text-green-300'
                            : 'bg-slate-500/20 text-slate-400'
                        }`}
                      >
                        {rule.isActive ? '启用' : '禁用'}
                      </span>
                    </td>
                    <td className="p-4 text-slate-400 text-sm">
                      {new Date(rule.updatedAt).toLocaleString('zh-CN')}
                    </td>
                    <td className="p-4 text-right space-x-2">
                      <button
                        onClick={() => handleEdit(rule)}
                        className="text-indigo-400 hover:text-indigo-300"
                      >
                        编辑
                      </button>
                      <button
                        onClick={() => handleToggleActive(rule)}
                        className="text-yellow-400 hover:text-yellow-300"
                      >
                        {rule.isActive ? '禁用' : '启用'}
                      </button>
                      <button
                        onClick={() => handleDelete(rule.id)}
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
          <div className="glass-card rounded-xl p-6 w-full max-w-2xl">
            <h2 className="text-2xl font-bold text-slate-200 mb-6">
              {editing ? '编辑规则' : '新建规则'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-slate-300 mb-2">规则名称 *</label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                  placeholder="例: 默认短信路由"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-slate-300 mb-2">消息类型 *</label>
                  <select
                    required
                    value={formData.messageType}
                    onChange={(e) =>
                      setFormData({ ...formData, messageType: e.target.value, manufacturerId: 0 })
                    }
                    className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                  >
                    <option value="SMS">短信</option>
                    <option value="Email">邮件</option>
                    <option value="AppPush">APP推送</option>
                  </select>
                </div>

                <div>
                  <label className="block text-slate-300 mb-2">目标厂商 *</label>
                  <select
                    required
                    value={formData.manufacturerId}
                    onChange={(e) =>
                      setFormData({ ...formData, manufacturerId: parseInt(e.target.value) })
                    }
                    className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                  >
                    <option value={0}>请选择厂商</option>
                    {getFilteredManufacturers().map((m) => (
                      <option key={m.id} value={m.id}>
                        {m.name}
                      </option>
                    ))}
                  </select>
                  {getFilteredManufacturers().length === 0 && (
                    <p className="text-yellow-400 text-xs mt-1">
                      当前消息类型没有可用的厂商
                    </p>
                  )}
                </div>
              </div>

              <div>
                <label className="block text-slate-300 mb-2">优先级 *</label>
                <input
                  type="number"
                  required
                  min={1}
                  max={100}
                  value={formData.priority}
                  onChange={(e) =>
                    setFormData({ ...formData, priority: parseInt(e.target.value) })
                  }
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500"
                />
                <p className="text-slate-500 text-xs mt-1">
                  数字越小优先级越高，范围: 1-100
                </p>
              </div>

              <div>
                <label className="block text-slate-300 mb-2">匹配条件 (JSON)</label>
                <textarea
                  value={formData.matchConditions}
                  onChange={(e) => setFormData({ ...formData, matchConditions: e.target.value })}
                  rows={4}
                  className="w-full bg-slate-800/50 border border-slate-700/50 rounded-lg px-4 py-2 text-slate-200 focus:outline-none focus:border-indigo-500 font-mono text-sm"
                  placeholder={'{\n  "tag": "marketing",\n  "region": "CN"\n}'}
                />
                <p className="text-slate-500 text-xs mt-1">
                  可选，用于更精细的路由控制，必须是有效的JSON格式
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
                  启用此规则
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
