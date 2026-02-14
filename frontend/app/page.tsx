'use client';

import { useEffect, useState } from 'react';

interface DashboardStats {
  total: number;
  successCount: number;
  failureCount: number;
  pendingCount: number;
  successRate: number;
  failureRate: number;
  todayTotal: number;
  todaySuccess: number;
  todaySuccessRate: number;
}

interface ManufacturerStat {
  manufacturerId?: number;
  manufacturerName: string;
  total: number;
  success: number;
  failure: number;
  pending: number;
  successRate: number;
}

interface MessageTypeStat {
  messageType: string;
  total: number;
  success: number;
  failure: number;
  pending: number;
  successRate: number;
}

export default function Home() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [manufacturers, setManufacturers] = useState<ManufacturerStat[]>([]);
  const [messageTypes, setMessageTypes] = useState<MessageTypeStat[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchDashboardData();
    const interval = setInterval(fetchDashboardData, 30000); // 每30秒刷新
    return () => clearInterval(interval);
  }, []);

  const fetchDashboardData = async () => {
    try {
      const [overviewRes, manufacturersRes, typesRes] = await Promise.all([
        fetch('http://localhost:5000/api/dashboard/overview'),
        fetch('http://localhost:5000/api/dashboard/manufacturers'),
        fetch('http://localhost:5000/api/dashboard/message-types')
      ]);

      const [overviewData, manufacturersData, typesData] = await Promise.all([
        overviewRes.json(),
        manufacturersRes.json(),
        typesRes.json()
      ]);

      if (overviewData.code === 200) {
        setStats(overviewData.data);
      }
      if (manufacturersData.code === 200) {
        setManufacturers(manufacturersData.data);
      }
      if (typesData.code === 200) {
        setMessageTypes(typesData.data);
      }
    } catch (error) {
      console.error('获取仪表盘数据失败:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-400 text-lg">加载中...</div>
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
          仪表板
        </h1>
        <button
          onClick={fetchDashboardData}
          className="px-4 py-2 bg-indigo-500/20 hover:bg-indigo-500/30 text-indigo-300 rounded-lg transition-colors"
        >
          刷新数据
        </button>
      </div>

      {/* 总览统计卡片 */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center justify-between mb-2">
            <span className="text-slate-400 text-sm">总发送量</span>
            <span className="text-2xl">📊</span>
          </div>
          <div className="text-3xl font-bold text-slate-100">{stats?.total || 0}</div>
          <div className="text-slate-400 text-xs mt-1">最近7天</div>
        </div>

        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center justify-between mb-2">
            <span className="text-slate-400 text-sm">成功率</span>
            <span className="text-2xl">✅</span>
          </div>
          <div className="text-3xl font-bold text-green-400">{stats?.successRate.toFixed(1) || 0}%</div>
          <div className="text-slate-400 text-xs mt-1">成功 {stats?.successCount || 0} 条</div>
        </div>

        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center justify-between mb-2">
            <span className="text-slate-400 text-sm">失败率</span>
            <span className="text-2xl">❌</span>
          </div>
          <div className="text-3xl font-bold text-red-400">{stats?.failureRate.toFixed(1) || 0}%</div>
          <div className="text-slate-400 text-xs mt-1">失败 {stats?.failureCount || 0} 条</div>
        </div>

        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center justify-between mb-2">
            <span className="text-slate-400 text-sm">今日发送</span>
            <span className="text-2xl">📅</span>
          </div>
          <div className="text-3xl font-bold text-blue-400">{stats?.todayTotal || 0}</div>
          <div className="text-slate-400 text-xs mt-1">成功率 {stats?.todaySuccessRate.toFixed(1) || 0}%</div>
        </div>
      </div>

      {/* 厂商统计 */}
      <div className="glass-card p-6 rounded-xl mb-6">
        <div className="flex items-center mb-4">
          <span className="text-2xl mr-3">🏭</span>
          <h2 className="text-xl font-semibold text-slate-100">厂商发送统计</h2>
        </div>
        {manufacturers.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-white/10">
                  <th className="text-left py-3 px-4 text-slate-400 font-medium">厂商</th>
                  <th className="text-right py-3 px-4 text-slate-400 font-medium">总量</th>
                  <th className="text-right py-3 px-4 text-slate-400 font-medium">成功</th>
                  <th className="text-right py-3 px-4 text-slate-400 font-medium">失败</th>
                  <th className="text-right py-3 px-4 text-slate-400 font-medium">成功率</th>
                </tr>
              </thead>
              <tbody>
                {manufacturers.map((mfr, index) => (
                  <tr key={index} className="border-b border-white/5 hover:bg-white/5 transition-colors">
                    <td className="py-3 px-4 text-slate-200">{mfr.manufacturerName || '未知厂商'}</td>
                    <td className="text-right py-3 px-4 text-slate-300">{mfr.total}</td>
                    <td className="text-right py-3 px-4 text-green-400">{mfr.success}</td>
                    <td className="text-right py-3 px-4 text-red-400">{mfr.failure}</td>
                    <td className="text-right py-3 px-4">
                      <span className={`px-2 py-1 rounded text-sm ${
                        mfr.successRate >= 95 ? 'bg-green-500/20 text-green-300' :
                        mfr.successRate >= 80 ? 'bg-yellow-500/20 text-yellow-300' :
                        'bg-red-500/20 text-red-300'
                      }`}>
                        {mfr.successRate.toFixed(1)}%
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-8 text-slate-400">暂无数据</div>
        )}
      </div>

      {/* 消息类型统计 */}
      <div className="glass-card p-6 rounded-xl mb-6">
        <div className="flex items-center mb-4">
          <span className="text-2xl mr-3">📨</span>
          <h2 className="text-xl font-semibold text-slate-100">消息类型统计</h2>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {messageTypes.map((type, index) => (
            <div key={index} className="bg-white/5 p-4 rounded-lg">
              <div className="flex items-center justify-between mb-2">
                <span className="text-slate-300 font-medium">{type.messageType}</span>
                <span className="text-2xl">
                  {type.messageType === 'SMS' ? '💬' : type.messageType === 'Email' ? '📧' : '📱'}
                </span>
              </div>
              <div className="text-2xl font-bold text-slate-100 mb-1">{type.total}</div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-green-400">成功 {type.success}</span>
                <span className="text-red-400">失败 {type.failure}</span>
              </div>
              <div className="mt-2 bg-white/10 rounded-full h-2 overflow-hidden">
                <div
                  className="h-full bg-gradient-to-r from-green-500 to-emerald-500"
                  style={{ width: `${type.successRate}%` }}
                ></div>
              </div>
              <div className="text-right text-xs text-slate-400 mt-1">{type.successRate.toFixed(1)}%</div>
            </div>
          ))}
          {messageTypes.length === 0 && (
            <div className="col-span-3 text-center py-8 text-slate-400">暂无数据</div>
          )}
        </div>
      </div>

      {/* 快捷入口 */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <a href="/manufacturers" className="glass-card p-6 rounded-xl hover:bg-white/10 transition-all">
          <div className="flex items-center mb-3">
            <span className="text-3xl mr-3">🏭</span>
            <h2 className="text-xl font-semibold text-slate-100">厂商管理</h2>
          </div>
          <p className="text-slate-300 text-sm">管理消息厂商和渠道配置</p>
        </a>

        <a href="/sms-templates" className="glass-card p-6 rounded-xl hover:bg-white/10 transition-all">
          <div className="flex items-center mb-3">
            <span className="text-3xl mr-3">📋</span>
            <h2 className="text-xl font-semibold text-slate-100">模板管理</h2>
          </div>
          <p className="text-slate-300 text-sm">配置短信和邮件模板</p>
        </a>

        <a href="/route-rules" className="glass-card p-6 rounded-xl hover:bg-white/10 transition-all">
          <div className="flex items-center mb-3">
            <span className="text-3xl mr-3">🔀</span>
            <h2 className="text-xl font-semibold text-slate-100">路由规则</h2>
          </div>
          <p className="text-slate-300 text-sm">设置消息路由逻辑</p>
        </a>

        <a href="/messages" className="glass-card p-6 rounded-xl hover:bg-white/10 transition-all">
          <div className="flex items-center mb-3">
            <span className="text-3xl mr-3">📨</span>
            <h2 className="text-xl font-semibold text-slate-100">消息记录</h2>
          </div>
          <p className="text-slate-300 text-sm">查看和管理已发送消息</p>
        </a>
      </div>
    </div>
  );
}
