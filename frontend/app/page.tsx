export default function Home() {
  return (
    <div>
      <h1 className="text-3xl font-bold mb-6 bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
        仪表板
      </h1>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center mb-4">
            <span className="text-3xl mr-3">🏭</span>
            <h2 className="text-xl font-semibold text-slate-100">厂商管理</h2>
          </div>
          <p className="text-slate-300 leading-relaxed">
            管理消息厂商和渠道配置
          </p>
        </div>
        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center mb-4">
            <span className="text-3xl mr-3">📋</span>
            <h2 className="text-xl font-semibold text-slate-100">模板管理</h2>
          </div>
          <p className="text-slate-300 leading-relaxed">
            配置短信和邮件模板
          </p>
        </div>
        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center mb-4">
            <span className="text-3xl mr-3">🔀</span>
            <h2 className="text-xl font-semibold text-slate-100">路由规则</h2>
          </div>
          <p className="text-slate-300 leading-relaxed">
            设置消息路由逻辑
          </p>
        </div>
        <div className="glass-card p-6 rounded-xl">
          <div className="flex items-center mb-4">
            <span className="text-3xl mr-3">📨</span>
            <h2 className="text-xl font-semibold text-slate-100">消息记录</h2>
          </div>
          <p className="text-slate-300 leading-relaxed">
            查看和管理已发送消息
          </p>
        </div>
      </div>
    </div>
  );
}
