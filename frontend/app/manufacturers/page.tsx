'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';

interface Manufacturer {
  id: number;
  name: string;
  code: string;
  providerType: string;
  supportedChannels: string;
  isActive: boolean;
  isConfigured: boolean;
  description?: string;
  configuration?: string;
  updatedAt?: string;
}

interface ConfigFormData {
  configuration: string;
  isActive: boolean;
  description: string;
}

export default function ManufacturersPage() {
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showConfigModal, setShowConfigModal] = useState(false);
  const [currentManufacturer, setCurrentManufacturer] = useState<Manufacturer | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [testing, setTesting] = useState(false);
  const [syncing, setSyncing] = useState(false);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);
  const [formData, setFormData] = useState<ConfigFormData>({
    configuration: '',
    isActive: false,
    description: '',
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

  const handleConfigure = async (manufacturer: Manufacturer) => {
    try {
      const result = await api.get(`/api/manufacturers/${manufacturer.id}`);
      if (result.code === 200) {
        setCurrentManufacturer(result.data);
        setFormData({
          configuration: result.data.configuration || '',
          isActive: result.data.isActive || false,
          description: result.data.description || '',
        });
        setShowConfigModal(true);
      }
    } catch (error) {
      showMessage('error', '加载厂商配置失败');
    }
  };

  const handleSaveConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentManufacturer) return;

    setSubmitting(true);
    try {
      const result = await api.put(`/api/manufacturers/${currentManufacturer.id}/config`, formData);
      if (result.code === 200) {
        showMessage('success', '配置保存成功');
        setShowConfigModal(false);
        loadManufacturers();
      } else {
        showMessage('error', result.msg || '配置保存失败');
      }
    } catch (error) {
      showMessage('error', '配置保存失败，请稍后重试');
    } finally {
      setSubmitting(false);
    }
  };

  const handleTestConnection = async (manufacturer: Manufacturer, channel: string) => {
    setTesting(true);
    try {
      const result = await api.post(`/api/manufacturers/${manufacturer.id}/test`, {
        channel: parseInt(channel)
      });
      if (result.code === 200) {
        showMessage('success', result.data?.message || '连接测试成功');
      } else {
        showMessage('error', result.msg || '连接测试失败');
      }
    } catch (error) {
      showMessage('error', '连接测试失败，请稍后重试');
    } finally {
      setTesting(false);
    }
  };

  const handleSyncTemplates = async (manufacturer: Manufacturer) => {
    setSyncing(true);
    try {
      const result = await api.post(`/api/manufacturers/${manufacturer.id}/sync-templates`, {});
      if (result.code === 200) {
        showMessage('success', `成功同步${result.data?.syncCount || 0}个模板`);
      } else {
        showMessage('error', result.msg || '模板同步失败');
      }
    } catch (error) {
      showMessage('error', '模板同步失败，请稍后重试');
    } finally {
      setSyncing(false);
    }
  };

  const getChannelBadges = (channels: string) => {
    return channels.split(',').map((ch) => {
      const channelMap: Record<string, { label: string; color: string }> = {
        'SMS': { label: '短信', color: 'bg-blue-500/20 text-blue-300 border-blue-500/30' },
        'Email': { label: '邮件', color: 'bg-green-500/20 text-green-300 border-green-500/30' },
        'AppPush': { label: '推送', color: 'bg-purple-500/20 text-purple-300 border-purple-500/30' },
      };
      const badge = channelMap[ch.trim()] || { label: ch, color: 'badge-info' };
      return (
        <span key={ch} className={`inline-block px-2 py-1 rounded-full text-xs font-medium border ${badge.color} mr-1`}>
          {badge.label}
        </span>
      );
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

      <div className="mb-6">
        <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
          厂商配置
        </h1>
        <p className="text-slate-400 mt-1">配置预设厂商的参数和凭证</p>
      </div>

      {/* 配置弹窗 */}
      {showConfigModal && currentManufacturer && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 flex items-center justify-center">
          <div className="glass-card p-8 rounded-xl max-w-2xl w-full mx-4 border-indigo-500/30">
            <h2 className="text-2xl font-semibold mb-6 text-slate-100">
              配置 {currentManufacturer.name}
            </h2>
            <form onSubmit={handleSaveConfig}>
              <div className="space-y-4 mb-6">
                <div>
                  <label className="block text-sm font-medium mb-2 text-slate-300">
                    配置信息 (JSON格式)
                  </label>
                  <textarea
                    value={formData.configuration}
                    onChange={(e) => setFormData({ ...formData, configuration: e.target.value })}
                    className="w-full rounded-lg px-4 py-2.5 font-mono text-sm"
                    rows={8}
                    placeholder='{"accessKeyId": "xxx", "accessKeySecret": "xxx"}'
                  />
                  <p className="text-xs text-slate-500 mt-1">
                    请输入厂商所需的配置参数，如AccessKey、SecretKey等
                  </p>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-2 text-slate-300">描述</label>
                  <input
                    type="text"
                    value={formData.description}
                    onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                    className="w-full rounded-lg px-4 py-2.5"
                    placeholder="可选的描述信息"
                  />
                </div>
                <div className="flex items-center">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="w-4 h-4 rounded border-slate-600"
                  />
                  <label htmlFor="isActive" className="ml-2 text-sm text-slate-300">
                    启用此厂商
                  </label>
                </div>
              </div>
              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={submitting}
                  className="btn-primary px-6 py-2.5 rounded-lg font-medium disabled:opacity-50"
                >
                  {submitting ? '保存中...' : '💾 保存配置'}
                </button>
                <button
                  type="button"
                  onClick={() => setShowConfigModal(false)}
                  className="btn-secondary px-6 py-2.5 rounded-lg font-medium"
                >
                  ✖️ 取消
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 厂商列表 */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {manufacturers.map((manufacturer) => (
          <div key={manufacturer.id} className="glass-card p-6 rounded-xl">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="text-xl font-semibold text-slate-100 mb-1">
                  {manufacturer.name}
                </h3>
                <p className="text-sm text-slate-400">{manufacturer.description}</p>
              </div>
              <div className="flex flex-col items-end gap-2">
                <span className={`badge ${manufacturer.isActive ? 'badge-success' : 'badge-error'}`}>
                  {manufacturer.isActive ? '已启用' : '未启用'}
                </span>
                {manufacturer.isConfigured && (
                  <span className="badge badge-info">已配置</span>
                )}
              </div>
            </div>

            <div className="mb-4">
              <div className="text-xs text-slate-500 mb-1">支持渠道:</div>
              <div>{getChannelBadges(manufacturer.supportedChannels)}</div>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => handleConfigure(manufacturer)}
                className="btn-primary px-4 py-2 text-sm rounded-lg"
              >
                ⚙️ 配置
              </button>
              {manufacturer.isConfigured && manufacturer.isActive && (
                <>
                  {manufacturer.supportedChannels.includes('SMS') && (
                    <>
                      <button
                        onClick={() => handleTestConnection(manufacturer, '1')}
                        disabled={testing}
                        className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                      >
                        🧪 测试短信
                      </button>
                      <button
                        onClick={() => handleSyncTemplates(manufacturer)}
                        disabled={syncing}
                        className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                      >
                        🔄 同步模板
                      </button>
                    </>
                  )}
                  {manufacturer.supportedChannels.includes('Email') && (
                    <button
                      onClick={() => handleTestConnection(manufacturer, '2')}
                      disabled={testing}
                      className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                    >
                      🧪 测试邮件
                    </button>
                  )}
                  {manufacturer.supportedChannels.includes('AppPush') && (
                    <button
                      onClick={() => handleTestConnection(manufacturer, '3')}
                      disabled={testing}
                      className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                    >
                      🧪 测试推送
                    </button>
                  )}
                </>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
