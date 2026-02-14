'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import ConfigField from '@/components/ConfigField';
import { useConfirm } from '@/components/ConfirmDialog';

interface Channel {
  id: number;
  name: string;
  code: string;
  channelType: string;
  channelTypeValue?: number;
  description?: string;
  supportedChannels: string;
  isActive: boolean;
  isConfigured: boolean;
  createdAt?: string;
  updatedAt?: string;
}

interface ChannelType {
  providerType: number;
  name: string;
  code: string;
  supportedChannels: string;
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

export default function ChannelsPage() {
  const [channels, setChannels] = useState<Channel[]>([]);
  const [channelTypes, setChannelTypes] = useState<ChannelType[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingChannel, setEditingChannel] = useState<Channel | null>(null);
  const [configSchema, setConfigSchema] = useState<ConfigurationSchema | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [testing, setTesting] = useState(false);
  const [syncing, setSyncing] = useState(false);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [showExample, setShowExample] = useState(false);
  const { confirm } = useConfirm();

  // 表单数据
  const [formValues, setFormValues] = useState<Record<string, string>>({});
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [description, setDescription] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [selectedChannelType, setSelectedChannelType] = useState<number | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [channelsResult, typesResult] = await Promise.all([
        api.get('/api/channels'),
        api.get('/api/channels/types')
      ]);

      if (channelsResult.code === 200) {
        setChannels(channelsResult.data);
      }

      if (typesResult.code === 200) {
        setChannelTypes(typesResult.data);
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
    setEditingChannel(null);
    setName('');
    setCode('');
    setDescription('');
    setIsActive(true);
    setSelectedChannelType(null);
    setConfigSchema(null);
    setFormValues({});
    setFormErrors({});
    setShowModal(true);
  };

  const handleEdit = async (channel: Channel) => {
    try {
      // 加载渠道详情
      const result = await api.get(`/api/channels/${channel.id}`);
      if (result.code !== 200) {
        showMessage('error', '加载渠道详情失败');
        return;
      }

      const channelData = result.data;
      setEditingChannel(channelData);
      setName(channelData.name);
      setCode(channelData.code);
      setDescription(channelData.description || '');
      setIsActive(channelData.isActive);
      setSelectedChannelType(channelData.channelTypeValue);

      // 加载配置Schema
      const schemaResult = await api.get(`/api/channels/types/${channelData.channelTypeValue}/config-schema`);
      if (schemaResult.code === 200) {
        setConfigSchema(schemaResult.data);
      }

      // 解析现有配置
      const existingConfig = channelData.configuration
        ? JSON.parse(channelData.configuration)
        : {};
      setFormValues(existingConfig);
      setFormErrors({});
      setShowModal(true);
    } catch (error) {
      showMessage('error', '加载渠道配置失败');
    }
  };

  const handleChannelTypeChange = async (typeValue: number) => {
    setSelectedChannelType(typeValue);

    try {
      const schemaResult = await api.get(`/api/channels/types/${typeValue}/config-schema`);
      if (schemaResult.code === 200) {
        setConfigSchema(schemaResult.data);
        setFormValues({});
        setFormErrors({});
      }
    } catch (error) {
      showMessage('error', '加载配置模板失败');
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

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!name || !code || selectedChannelType === null) {
      showMessage('error', '请填写必填项');
      return;
    }

    // 验证配置字段
    if (configSchema) {
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
    }

    setSubmitting(true);
    try {
      const configData = { ...formValues };
      const payload = {
        name,
        code,
        channelType: selectedChannelType,
        description: description || null,
        configuration: Object.keys(configData).length > 0 ? JSON.stringify(configData) : null,
        isActive
      };

      let result;
      if (editingChannel) {
        result = await api.put(`/api/channels/${editingChannel.id}`, payload);
      } else {
        result = await api.post('/api/channels', payload);
      }

      if (result.code === 200) {
        showMessage('success', editingChannel ? '渠道更新成功' : '渠道创建成功');
        setShowModal(false);
        loadData();
      } else {
        showMessage('error', result.msg || '操作失败');
      }
    } catch (error) {
      showMessage('error', '操作失败，请稍后重试');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (channel: Channel) => {
    const confirmed = await confirm({
      title: '确认删除',
      message: `确定要删除渠道"${channel.name}"吗？此操作不可撤销。`,
      confirmText: '删除',
      cancelText: '取消'
    });

    if (!confirmed) {
      return;
    }

    try {
      const result = await api.delete(`/api/channels/${channel.id}`);
      if (result.code === 200) {
        showMessage('success', '渠道删除成功');
        loadData();
      } else {
        showMessage('error', result.msg || '删除失败');
      }
    } catch (error) {
      showMessage('error', '删除失败，请稍后重试');
    }
  };

  const handleTest = async (channel: Channel, messageChannel: number) => {
    setTesting(true);
    try {
      const result = await api.post(`/api/channels/${channel.id}/test`, {
        channel: messageChannel
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

  const handleSync = async (channel: Channel) => {
    setSyncing(true);
    try {
      const result = await api.post(`/api/channels/${channel.id}/sync-templates`, {});
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

      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
            渠道管理
          </h1>
          <p className="text-slate-400 mt-1">管理消息发送渠道配置</p>
        </div>
        <button
          onClick={handleCreate}
          className="btn-primary px-6 py-2.5 rounded-lg font-medium"
        >
          ➕ 新建渠道
        </button>
      </div>

      {/* 配置弹窗 */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 flex items-center justify-center p-4 overflow-y-auto">
          <div className="glass-card p-8 rounded-xl max-w-3xl w-full mx-4 my-8 border-indigo-500/30">
            <div className="flex items-start justify-between mb-6">
              <div>
                <h2 className="text-2xl font-semibold text-slate-100">
                  {editingChannel ? '编辑渠道' : '新建渠道'}
                </h2>
                {configSchema?.description && (
                  <p className="text-sm text-slate-400 mt-1">{configSchema.description}</p>
                )}
                {configSchema?.documentationUrl && (
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
                onClick={() => setShowModal(false)}
                className="text-slate-400 hover:text-slate-200 transition-colors text-2xl leading-none"
              >
                ×
              </button>
            </div>

            <form onSubmit={handleSave}>
              <div className="space-y-6 mb-6 max-h-[60vh] overflow-y-auto pr-2">
                {/* 基本信息 */}
                <div className="space-y-4">
                  <div className="flex items-center gap-2 pb-2 border-b border-slate-600/30">
                    <h3 className="text-sm font-semibold text-slate-300">基本信息</h3>
                  </div>

                  <div className="space-y-2">
                    <label className="block text-sm font-medium text-slate-300">
                      渠道名称 <span className="text-red-400">*</span>
                    </label>
                    <input
                      type="text"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      className="w-full rounded-lg px-4 py-2.5 bg-slate-800/50 border border-slate-600/50 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-transparent transition-all"
                      placeholder="例如：阿里云短信-生产环境"
                      required
                    />
                  </div>

                  <div className="space-y-2">
                    <label className="block text-sm font-medium text-slate-300">
                      渠道编码 <span className="text-red-400">*</span>
                    </label>
                    <input
                      type="text"
                      value={code}
                      onChange={(e) => setCode(e.target.value)}
                      className="w-full rounded-lg px-4 py-2.5 bg-slate-800/50 border border-slate-600/50 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-transparent transition-all"
                      placeholder="例如：aliyun_sms_prod"
                      required
                      disabled={!!editingChannel}
                    />
                    <p className="text-xs text-slate-500">唯一标识，创建后不可修改</p>
                  </div>

                  <div className="space-y-2">
                    <label className="block text-sm font-medium text-slate-300">
                      渠道类型 <span className="text-red-400">*</span>
                    </label>
                    <select
                      value={selectedChannelType || ''}
                      onChange={(e) => handleChannelTypeChange(parseInt(e.target.value))}
                      className="w-full rounded-lg px-4 py-2.5 bg-slate-800/50 border border-slate-600/50 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-transparent transition-all"
                      required
                      disabled={!!editingChannel}
                    >
                      <option value="">请选择渠道类型</option>
                      {channelTypes.map(type => (
                        <option key={type.providerType} value={type.providerType}>
                          {type.name} ({type.supportedChannels})
                        </option>
                      ))}
                    </select>
                    <p className="text-xs text-slate-500">选择渠道类型后将显示对应的配置项</p>
                  </div>

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
                </div>

                {/* 配置字段 */}
                {configSchema && (
                  <>
                    {Object.entries(fieldGroups).map(([groupName, fields]) => {
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

                    {/* 高级配置 */}
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
                  </>
                )}

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
                    <span className="font-medium">启用此渠道</span>
                    <span className="block text-xs text-slate-400 mt-0.5">
                      启用后，该渠道将可以在路由规则中使用
                    </span>
                  </label>
                </div>
              </div>

              <div className="flex gap-3 pt-4 border-t border-slate-600/30">
                <button
                  type="submit"
                  disabled={submitting}
                  className="btn-primary px-6 py-2.5 rounded-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed flex-1"
                >
                  {submitting ? '保存中...' : '💾 保存'}
                </button>
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="btn-secondary px-6 py-2.5 rounded-lg font-medium"
                >
                  取消
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 渠道列表 */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {channels.map((channel) => (
          <div key={channel.id} className="glass-card p-6 rounded-xl">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="text-xl font-semibold text-slate-100 mb-1">
                  {channel.name}
                </h3>
                <p className="text-sm text-slate-400">{channel.description || channel.code}</p>
                <p className="text-xs text-slate-500 mt-1">类型：{channel.channelType}</p>
              </div>
              <div className="flex flex-col items-end gap-2">
                <span className={`badge ${channel.isActive ? 'badge-success' : 'badge-error'}`}>
                  {channel.isActive ? '已启用' : '未启用'}
                </span>
                {channel.isConfigured && (
                  <span className="badge badge-info">已配置</span>
                )}
              </div>
            </div>

            <div className="mb-4">
              <div className="text-xs text-slate-500 mb-1">支持渠道:</div>
              <div>{getChannelBadges(channel.supportedChannels)}</div>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => handleEdit(channel)}
                className="btn-primary px-4 py-2 text-sm rounded-lg"
              >
                ⚙️ 编辑
              </button>
              <button
                onClick={() => handleDelete(channel)}
                className="btn-secondary px-4 py-2 text-sm rounded-lg"
              >
                🗑️ 删除
              </button>
              {channel.isConfigured && channel.isActive && (
                <>
                  {channel.supportedChannels.includes('SMS') && (
                    <>
                      <button
                        onClick={() => handleTest(channel, 1)}
                        disabled={testing}
                        className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                      >
                        🧪 测试短信
                      </button>
                      <button
                        onClick={() => handleSync(channel)}
                        disabled={syncing}
                        className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                      >
                        🔄 同步模板
                      </button>
                    </>
                  )}
                  {channel.supportedChannels.includes('Email') && (
                    <button
                      onClick={() => handleTest(channel, 2)}
                      disabled={testing}
                      className="btn-secondary px-4 py-2 text-sm rounded-lg disabled:opacity-50"
                    >
                      🧪 测试邮件
                    </button>
                  )}
                  {channel.supportedChannels.includes('AppPush') && (
                    <button
                      onClick={() => handleTest(channel, 3)}
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

      {channels.length === 0 && (
        <div className="text-center py-16 text-slate-400">
          <p className="mb-4">暂无渠道配置</p>
          <button
            onClick={handleCreate}
            className="btn-primary px-6 py-2.5 rounded-lg font-medium"
          >
            ➕ 创建第一个渠道
          </button>
        </div>
      )}
    </div>
  );
}
