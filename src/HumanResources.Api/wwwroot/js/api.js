/**
 * TalentPulse — API Service Module
 * Handles centralized HTTP communication, Bearer authentication, and error normalization.
 */

const API_BASE = '/api';

class ApiClient {
  static getToken() {
    return sessionStorage.getItem('tp_jwt_token');
  }

  static setToken(token) {
    sessionStorage.setItem('tp_jwt_token', token);
  }

  static clearToken() {
    sessionStorage.removeItem('tp_jwt_token');
  }

  static async request(endpoint, options = {}) {
    const url = `${API_BASE}${endpoint}`;
    const token = this.getToken();

    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      ...(options.headers || {})
    };

    const config = {
      ...options,
      headers
    };

    try {
      const response = await fetch(url, config);
      const isJson = response.headers.get('content-type')?.includes('application/json');
      const data = isJson ? await response.json() : null;

      if (!response.ok) {
        const errorMessage = data?.error || data?.message || `HTTP ${response.status}: ${response.statusText}`;
        throw new Error(errorMessage);
      }

      return data;
    } catch (err) {
      console.error(`[API Error] ${endpoint}:`, err);
      throw err;
    }
  }

  // Auth Endpoints
  static async login(email, password) {
    const result = await this.request('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password })
    });
    if (result?.token) {
      this.setToken(result.token);
    }
    return result;
  }

  // Leave Endpoints
  static async getMyLeaves() {
    return this.request('/leaves/my');
  }

  static async getAllLeaves(status = null) {
    const query = status ? `?status=${status}` : '';
    return this.request(`/leaves${query}`);
  }

  static async submitLeave(leaveData) {
    return this.request('/leaves', {
      method: 'POST',
      body: JSON.stringify(leaveData)
    });
  }

  static async approveLeave(leaveId) {
    return this.request(`/leaves/${leaveId}/approve`, {
      method: 'POST'
    });
  }

  static async rejectLeave(leaveId, reason) {
    return this.request(`/leaves/${leaveId}/reject`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    });
  }

  static async cancelLeave(leaveId) {
    return this.request(`/leaves/${leaveId}/cancel`, {
      method: 'POST'
    });
  }

  // Employee & Department Endpoints
  static async getEmployees() {
    return this.request('/employees');
  }

  static async getDepartments() {
    return this.request('/departments');
  }
}

window.ApiClient = ApiClient;
