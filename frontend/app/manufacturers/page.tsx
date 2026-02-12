'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import ConfigField from '@/components/ConfigField';

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

interface ConfigurationField {
  name: string;
  label: string;
  type: string;
  required: boolean;
  defaultValue?: string;
  placeholder?: string;
  helpText?: string;
  validationPattern?: string;
  validationMessage?: string;
  options?: Array<{ label: string; value: string }>;
  isSensitive: boolean;
  group?: string;
  order: number;
}

interface ConfigurationSchema {
  providerName: string;
  description?: string;
  documentationUrl?: string;
  fields: ConfigurationField[];
  example?: string;
}

export default function ManufacturersPage() {
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showConfigModal, setShowConfigModal] = useState(false);
  const [currentManufacturer, setCurrentManufacturer] = useState<Manufacturer | null>(null);
  const [configSchema, setConfigSchema] = useState<ConfigurationSchema | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [testing, setTesting] = useState(false);
  const [syncing, setSyncing] = useState(false);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [showExample, setShowExample] = useState(false);

  // 结构化表单数据
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [isActive, setIsActive] = useState(false);
  const [description, setDescription] = useState('');

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
      // 获取配置Schema
      const schemaResult = await api.get(`/api/manufacturers/${manufacturer.id}/config-schema`);
      if (schemaResult.code !== 200) {
        showMessage('error', '获取配置信息失败');
        return;
      }

      // 获取当前配置
      const configResult = await api.get(`/api/manufacturers/${manufacturer.id}`);
      if (configResult.code !== 200) {
        showMessage('error', '加载厂商配置失败');
        return;
      }

      setCurrentManufacturer(configResult.data);
      setConfigSchema(schemaResult.data);
      setIsActive(configResult.data.isActive || false);
      setDescription(configResult.data.description || '');

      // 解析现有配置到表单字段
      const existingConfig = configResult.data.configuration
        ? JSON.parse(configResult.data.configuration)
        : {};
      setFormValues(existingConfig);
      setFormErrors({});
      setShowConfigModal(true);
    } catch (error) {
      showMessage('error', '加载配置失败');
    }
  };

  const validateField = (field: ConfigurationField, value: string): string | undefined => {
    if (field.required && !value) {
      return `${field.label}为必填项`;
    }

    if (value && field.validationPattern) {
      const regex = new RegExp(field.validationPattern);
      if (!regex.test(value)) {
        return field.validationMessage || `${field.label}格式不正确`;
      }
    }

    return undefined;
  };

  const handleSaveConfig = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentManufacturer || !configSchema) return;

    // 验证所有字段
    const errors: Record<string, string> = {};
    configSchema.fields.forEach(field => {
      const error = validateField(field, formValues[field.name] || '');
      if (error) {
        errors[field.name] = error;
      }
    });

    if (Object.keys(errors).length > 0) {
      setFormErrors(errors);
      showMessage('error', '请修正表单中的错误');
      return;
    }

    setSubmitting(true);
    try {
      const configData = { ...formValues };
      const result = await api.put(`/api/manufacturers/${currentManufacturer.id}/config`, {
        configuration: JSON.stringify(configData),
        isActive,
        description
      });

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

  // 按分组整理字段
  const getFieldsByGroup = (): Record<string, ConfigurationField[]> => {
    if (!configSchema) return {};

    const groups: Record<string, ConfigurationField[]> = {};
    configSchema.fields.forEach(field => {
      const groupName = field.group || '基本配置';
      if (!groups[groupName]) {
        groups[groupName] = [];
      }
      groups[groupName].push(field);
    });

    // 按order排序每个组内的字段
    Object.keys(groups).forEach(group => {
      groups[group].sort((a, b) => a.order - b.order);
    });

    return groups;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-400">加载中...</div>
      </div>
    );
  }

  const fieldGroups = getFieldsByGroup();
  const advancedGroupName = '高级配置';

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
      {showConfigModal && currentManufacturer && configSchema && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 flex items-center justify-center p-4 overflow-y-auto">
          <div className="glass-card p-8 rounded-xl max-w-3xl w-full mx-4 my-8 border-indigo-500/30">
            <div className="flex items-start justify-between mb-6">
              <div>
                <h2 className="text-2xl font-semibold text-slate-100">
                  配置 {configSchema.providerName}
                </h2>
                {configSchema.description && (
                  <p className="text-sm text-slate-400 mt-1">{configSchema.description}</p>
                )}
                {configSchema.documentationUrl && (
                  <a
                    href={configSchema.documentationUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-xs text-indigo-400 hover:text-indigo-300 mt-1 inline-flex items-center gap-1"
                  >
                    📚 查看官方文档 →
                  </a>
                )}
              </div>
              <button
                onClick={() => setShowConfigModal(false)}
                className="text-slate-400 hover:text-slate-200 transition-colors text-2xl leading-none"
              >
                ×
              </button>
            </div>

            <form onSubmit={handleSaveConfig}>
              <div className="space-y-6 mb-6 max-h-[60vh] overflow-y-auto pr-2">
                {/* 基本字段和认证信息 */}
                {Object.entries(fieldGroups).map(([groupName, fields]) => {
                  // 高级配置单独处理
                  if (groupName === advancedGroupName) return null;

                  return (
                    <div key={groupName} className="space-y-4">
                      <div className="flex items-center gap-2 pb-2 border-b border-slate-600/30">
                        <h3 className="text-sm font-semibold text-slate-300">{groupName}</h3>
                      </div>
                      {fields.map(field => (
                        <ConfigField
                          key={field.name}
                          field={field}
                          value={formValues[field.name] || ''}
                          onChange={(value) => {
                            setFormValues({ ...formValues, [field.name]: value });
                            // 清除该字段错误
                            if (formErrors[field.name]) {
                              const newErrors = { ...formErrors };
                              delete newErrors[field.name];
                              setFormErrors(newErrors);
                            }
                          }}
                          error={formErrors[field.name]}
                        />
                      ))}
                    </div>
                  );
                })}

                {/* 高级配置（可折叠） */}
                {fieldGroups[advancedGroupName] && (
                  <div className="space-y-4">
                    <button
                      type="button"
                      onClick={() => setShowAdvanced(!showAdvanced)}
                      className="flex items-center gap-2 text-sm font-semibold text-slate-300 hover:text-slate-100 transition-colors"
                    >
                      <span className={`transform transition-transform ${showAdvanced ? 'rotate-90' : ''}`}>▶</span>
                      {advancedGroupName}
                      <span className="text-xs text-slate-500">(可选)</span>
                    </button>
                    {showAdvanced && (
                      <div className="space-y-4 pl-4 border-l-2 border-slate-600/30">
                        {fieldGroups[advancedGroupName].map(field => (
                          <ConfigField
                            key={field.name}
                            field={field}
                            value={formValues[field.name] || field.defaultValue || ''}
                            onChange={(value) => {
                              setFormValues({ ...formValues, [field.name]: value });
                              if (formErrors[field.name]) {
                                const newErrors = { ...formErrors };
                                delete newErrors[field.name];
                                setFormErrors(newErrors);
                              }
                            }}
                            error={formErrors[field.name]}
                          />
                        ))}
                      </div>
                    )}
                  </div>
                )}

                {/* 描述 */}
                <div className="space-y-2">
                  <label className="block text-sm font-medium text-slate-300">描述</label>
                  <input
                    type="text"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="w-full rounded-lg px-4 py-2.5 bg-slate-800/50 border border-slate-600/50 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-transparent transition-all"
                    placeholder="可选的描述信息"
                  />
                </div>

                {/* 启用状态 */}
                <div className="flex items-center gap-3 p-4 bg-slate-700/20 rounded-lg border border-slate-600/30">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={isActive}
                    onChange={(e) => setIsActive(e.target.checked)}
                    className="w-5 h-5 rounded border-slate-600 text-indigo-500 focus:ring-2 focus:ring-indigo-500/50 cursor-pointer"
                  />
                  <label htmlFor="isActive" className="text-sm text-slate-300 cursor-pointer flex-1">
                    <span className="font-medium">启用此厂商</span>
                    <span className="block text-xs text-slate-400 mt-0.5">
                      启用后，该厂商将可以在路由规则中使用
                    </span>
                  </label>
                </div>

                {/* 配置示例 */}
                {configSchema.example && (
                  <div className="space-y-2">
                    <button
                      type="button"
                      onClick={() => setShowExample(!showExample)}
                      className="text-xs text-indigo-400 hover:text-indigo-300 flex items-center gap-1"
                    >
                      <span className={`transform transition-transform ${showExample ? 'rotate-90' : ''}`}>▶</span>
                      查看配置示例
                    </button>
                    {showExample && (
                      <pre className="text-xs bg-slate-900/50 p-4 rounded-lg border border-slate-600/30 overflow-x-auto text-slate-300 font-mono">
                        {configSchema.example}
                      </pre>
                    )}
                  </div>
                )}
              </div>

              <div className="flex gap-3 pt-4 border-t border-slate-600/30">
                <button
                  type="submit"
                  disabled={submitting}
                  className="btn-primary px-6 py-2.5 rounded-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed flex-1"
                >
                  {submitting ? '保存中...' : '💾 保存配置'}
                </button>
                <button
                  type="button"
                  onClick={() => setShowConfigModal(false)}
                  className="btn-secondary px-6 py-2.5 rounded-lg font-medium"
                >
                  取消
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
