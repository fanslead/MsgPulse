'use client';

export default function EmailTemplatesPage() {
  return (
    <div>
      <div className="mb-6">
        <h1 className="text-3xl font-bold bg-gradient-to-r from-indigo-400 to-purple-400 bg-clip-text text-transparent">
          邮件模板
        </h1>
        <p className="text-slate-400 mt-1">管理邮件模板配置</p>
      </div>
      <div className="glass-card p-12 rounded-xl text-center">
        <div className="text-6xl mb-4">✉️</div>
        <p className="text-slate-300 text-lg">邮件模板管理功能开发中...</p>
        <p className="text-slate-400 text-sm mt-2">参考厂商管理页面实现完整CRUD功能</p>
      </div>
    </div>
  );
}
