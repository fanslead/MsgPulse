const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export const api = {
  async get(path: string) {
    const res = await fetch(`${API_BASE_URL}${path}`);
    return res.json();
  },

  async post(path: string, data: any) {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return res.json();
  },

  async put(path: string, data: any) {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    return res.json();
  },

  async delete(path: string) {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      method: 'DELETE',
    });
    return res.json();
  },
};
