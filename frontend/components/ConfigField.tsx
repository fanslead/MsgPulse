'use client';

import { useState } from 'react';

interface ConfigFieldProps {
  field: {
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
  };
  value: string;
  onChange: (value: string) => void;
  error?: string;
}

export default function ConfigField({ field, value, onChange, error }: ConfigFieldProps) {
  const [showPassword, setShowPassword] = useState(false);

  const inputBaseClass = "w-full rounded-lg px-4 py-2.5 bg-slate-800/50 border border-slate-600/50 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-transparent transition-all";
  const errorClass = error ? "border-red-500/70 focus:ring-red-500/50" : "";

  const renderInput = () => {
    if (field.type === 'select' && field.options) {
      return (
        <select
          value={value || field.defaultValue || ''}
          onChange={(e) => onChange(e.target.value)}
          className={`${inputBaseClass} ${errorClass} cursor-pointer`}
          required={field.required}
        >
          <option value="">请选择...</option>
          {field.options.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      );
    }

    if (field.type === 'password') {
      return (
        <div className="relative">
          <input
            type={showPassword ? 'text' : 'password'}
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            className={`${inputBaseClass} ${errorClass} pr-12`}
            placeholder={field.placeholder}
            required={field.required}
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors text-sm"
          >
            {showPassword ? '🙈 隐藏' : '👁️ 显示'}
          </button>
        </div>
      );
    }

    if (field.type === 'number') {
      return (
        <input
          type="number"
          value={value || ''}
          onChange={(e) => onChange(e.target.value)}
          className={`${inputBaseClass} ${errorClass}`}
          placeholder={field.placeholder}
          required={field.required}
        />
      );
    }

    // Default: text
    return (
      <input
        type="text"
        value={value || ''}
        onChange={(e) => onChange(e.target.value)}
        className={`${inputBaseClass} ${errorClass}`}
        placeholder={field.placeholder}
        required={field.required}
        pattern={field.validationPattern}
      />
    );
  };

  return (
    <div className="space-y-2">
      <label className="block text-sm font-medium text-slate-300">
        {field.label}
        {field.required && <span className="text-red-400 ml-1">*</span>}
        {field.isSensitive && (
          <span className="ml-2 text-xs px-2 py-0.5 bg-amber-500/20 text-amber-300 rounded-full border border-amber-500/30">
            🔒 敏感信息
          </span>
        )}
      </label>

      {renderInput()}

      {field.helpText && (
        <p className="text-xs text-slate-400 flex items-start gap-1">
          <span className="text-indigo-400 mt-0.5">💡</span>
          <span>{field.helpText}</span>
        </p>
      )}

      {error && (
        <p className="text-xs text-red-400 flex items-start gap-1">
          <span className="mt-0.5">⚠️</span>
          <span>{error}</span>
        </p>
      )}
    </div>
  );
}
