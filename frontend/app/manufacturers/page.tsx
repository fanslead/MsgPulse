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
      console.error('Failed to load manufacturers:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingId) {
        await api.put(`/api/manufacturers/${editingId}`, formData);
      } else {
        await api.post('/api/manufacturers', formData);
      }
      setShowForm(false);
      setEditingId(null);
      resetForm();
      loadManufacturers();
    } catch (error) {
      console.error('Failed to save manufacturer:', error);
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

  const handleDelete = async (id: number) => {
    if (confirm('Are you sure you want to delete this manufacturer?')) {
      try {
        await api.delete(`/api/manufacturers/${id}`);
        loadManufacturers();
      } catch (error) {
        console.error('Failed to delete manufacturer:', error);
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
    return <div>Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Manufacturers</h1>
        <button
          onClick={() => {
            setShowForm(true);
            setEditingId(null);
            resetForm();
          }}
          className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
        >
          Add Manufacturer
        </button>
      </div>

      {showForm && (
        <div className="bg-white p-6 rounded-lg shadow mb-6">
          <h2 className="text-xl font-semibold mb-4">
            {editingId ? 'Edit Manufacturer' : 'Add Manufacturer'}
          </h2>
          <form onSubmit={handleSubmit}>
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div>
                <label className="block text-sm font-medium mb-1">Name</label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full border rounded px-3 py-2"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Code</label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                  className="w-full border rounded px-3 py-2"
                  required
                />
              </div>
              <div className="col-span-2">
                <label className="block text-sm font-medium mb-1">Description</label>
                <input
                  type="text"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full border rounded px-3 py-2"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Supported Channels</label>
                <select
                  value={formData.supportedChannels}
                  onChange={(e) => setFormData({ ...formData, supportedChannels: e.target.value })}
                  className="w-full border rounded px-3 py-2"
                >
                  <option value="SMS">SMS</option>
                  <option value="Email">Email</option>
                  <option value="AppPush">App Push</option>
                  <option value="SMS,Email">SMS,Email</option>
                  <option value="SMS,Email,AppPush">All</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Status</label>
                <select
                  value={formData.isActive.toString()}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.value === 'true' })}
                  className="w-full border rounded px-3 py-2"
                >
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
            </div>
            <div className="flex gap-2">
              <button
                type="submit"
                className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600"
              >
                Save
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowForm(false);
                  setEditingId(null);
                  resetForm();
                }}
                className="bg-gray-300 text-gray-700 px-4 py-2 rounded hover:bg-gray-400"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="bg-white rounded-lg shadow overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-100">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Code</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Channels</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {manufacturers.map((manufacturer) => (
              <tr key={manufacturer.id}>
                <td className="px-6 py-4 whitespace-nowrap">{manufacturer.name}</td>
                <td className="px-6 py-4 whitespace-nowrap">{manufacturer.code}</td>
                <td className="px-6 py-4 whitespace-nowrap">{manufacturer.supportedChannels}</td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 py-1 text-xs rounded ${manufacturer.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                    {manufacturer.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap space-x-2">
                  <button
                    onClick={() => handleEdit(manufacturer)}
                    className="text-blue-600 hover:text-blue-800"
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => handleDelete(manufacturer.id)}
                    className="text-red-600 hover:text-red-800"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
