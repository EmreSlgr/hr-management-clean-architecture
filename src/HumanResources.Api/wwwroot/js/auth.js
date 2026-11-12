/**
 * TalentPulse — Authentication & Persona Manager
 * Manages active user context, pre-seeded accounts, and session tokens.
 */

const PRESET_PERSONAS = [
  {
    id: 'emre',
    name: 'Emre Salgır',
    role: 'Employee',
    roleLabel: 'Senior Software Developer',
    email: 'emre.salgir@company.com',
    password: 'Password123!'
  },
  {
    id: 'sarah',
    name: 'Sarah Connor',
    role: 'HRManager',
    roleLabel: 'People & Culture Lead',
    email: 'sarah.hr@company.com',
    password: 'Password123!'
  },
  {
    id: 'admin',
    name: 'System Admin',
    role: 'Admin',
    roleLabel: 'Chief Technology Officer',
    email: 'admin@company.com',
    password: 'Password123!'
  },
  {
    id: 'michael',
    name: 'Michael Scott',
    role: 'Employee',
    roleLabel: 'Operations Specialist',
    email: 'michael.scott@company.com',
    password: 'Password123!'
  }
];

class AuthManager {
  static currentUser = null;
  static listeners = [];

  static getPersonas() {
    return PRESET_PERSONAS;
  }

  static getCurrentUser() {
    return this.currentUser;
  }

  static subscribe(fn) {
    this.listeners.push(fn);
  }

  static notify() {
    for (const fn of this.listeners) {
      fn(this.currentUser);
    }
  }

  static async switchPersona(personaId) {
    const persona = PRESET_PERSONAS.find(p => p.id === personaId) || PRESET_PERSONAS[0];
    try {
      const authResult = await ApiClient.login(persona.email, persona.password);
      this.currentUser = {
        ...persona,
        ...authResult
      };
      sessionStorage.setItem('tp_active_persona', persona.id);
      this.notify();
      return this.currentUser;
    } catch (err) {
      console.error('Failed to authenticate persona:', err);
      throw err;
    }
  }

  static async init() {
    const savedPersona = sessionStorage.getItem('tp_active_persona') || 'emre';
    return await this.switchPersona(savedPersona);
  }
}

window.AuthManager = AuthManager;
