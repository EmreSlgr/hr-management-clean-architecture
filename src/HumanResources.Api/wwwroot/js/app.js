/**
 * Main Application Controller & State Engine
 * High-Contrast Enterprise Architecture
 */

class App {
  static currentTab = 'dashboard';
  static myLeaves = [];
  static companyLeaves = [];
  static employees = [];
  static departments = [];
  static pendingRejectLeaveId = null;

  static async init() {
    this.setupEventListeners();
    this.populatePersonaSelect();

    try {
      await AuthManager.init();
      await this.refreshAllData();
      this.switchTab('dashboard');
    } catch (err) {
      console.error('App initialization error:', err);
      UIComponents.showToast('Başlatma hatası: ' + err.message, 'error');
    }
  }

  static populatePersonaSelect() {
    const select = document.getElementById('persona-select');
    if (!select) return;

    const personas = AuthManager.getPersonas();
    select.innerHTML = personas.map(p => `
      <option value="${p.id}">${p.name} (${p.role})</option>
    `).join('');

    const activeId = sessionStorage.getItem('tp_active_persona') || 'emre';
    select.value = activeId;
  }

  static setupEventListeners() {
    // Persona Switcher
    const select = document.getElementById('persona-select');
    if (select) {
      select.addEventListener('change', async (e) => {
        const user = await AuthManager.switchPersona(e.target.value);
        UIComponents.showToast(`${user.name} (${user.role}) oturumu açıldı.`, 'info');
        await this.refreshAllData();
      });
    }

    // Tab Navigation
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        const tab = btn.dataset.tab;
        this.switchTab(tab);
      });
    });

    // Leave Modal Form Date Calculation
    const startDateInput = document.getElementById('leave-start-date');
    const endDateInput = document.getElementById('leave-end-date');
    const typeSelect = document.getElementById('leave-type-select');

    const updateDayCalculation = () => {
      const start = new Date(startDateInput.value);
      const end = new Date(endDateInput.value);
      const alertBox = document.getElementById('leave-balance-alert');

      if (!isNaN(start) && !isNaN(end) && end >= start) {
        const days = Math.round((end - start) / (1000 * 60 * 60 * 24)) + 1;
        document.getElementById('calculated-days').textContent = `${days} gün`;

        const user = AuthManager.getCurrentUser();
        const leaveType = parseInt(typeSelect.value, 10);

        // Domain invariant preview: Annual leave balance check
        if (leaveType === 1 && user && days > user.annualLeaveBalance) {
          alertBox.textContent = `Talep edilen ${days} gün, mevcut ${user.annualLeaveBalance} günlük yıllık izin bakiyenizi aşmaktadır. Bu başvuru sunulduğunda API kural denetimiyle reddedilecektir.`;
          alertBox.className = 'alert-box warning';
        } else {
          alertBox.className = 'alert-box';
        }
      } else {
        document.getElementById('calculated-days').textContent = '0 gün';
        alertBox.className = 'alert-box';
      }
    };

    startDateInput?.addEventListener('change', updateDayCalculation);
    endDateInput?.addEventListener('change', updateDayCalculation);
    typeSelect?.addEventListener('change', updateDayCalculation);

    // Leave Form Submission
    const leaveForm = document.getElementById('leave-request-form');
    leaveForm?.addEventListener('submit', async (e) => {
      e.preventDefault();
      await this.handleCreateLeave();
    });

    // Reject Form Submission
    const rejectForm = document.getElementById('reject-form');
    rejectForm?.addEventListener('submit', async (e) => {
      e.preventDefault();
      await this.handleConfirmReject();
    });
  }

  static switchTab(tabId) {
    this.currentTab = tabId;

    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
    document.querySelectorAll('.tab-view').forEach(v => v.classList.remove('active'));

    const activeBtn = document.querySelector(`.tab-btn[data-tab="${tabId}"]`);
    const activeView = document.getElementById(`tab-view-${tabId}`);

    if (activeBtn) activeBtn.classList.add('active');
    if (activeView) activeView.classList.add('active');

    this.renderCurrentView();
  }

  static async refreshAllData() {
    const user = AuthManager.getCurrentUser();
    if (!user) return;

    // Update header profile snippet
    document.getElementById('current-user-name').textContent = user.fullName || user.name;
    const roleElem = document.getElementById('current-user-role');
    roleElem.textContent = user.role;
    roleElem.className = `role-tag ${user.role.toLowerCase()}`;

    // Manage HR Inbox Visibility
    const isHrOrAdmin = user.role === 'HRManager' || user.role === 'Admin';
    const hrTabBtn = document.querySelector('.tab-btn[data-tab="inbox"]');
    if (hrTabBtn) {
      hrTabBtn.style.display = isHrOrAdmin ? 'flex' : 'none';
    }

    try {
      this.myLeaves = await ApiClient.getMyLeaves();

      if (isHrOrAdmin) {
        this.companyLeaves = await ApiClient.getAllLeaves();
        const pendingCount = this.companyLeaves.filter(l => l.status === 'Pending').length;
        const countBadge = document.getElementById('inbox-pending-count');
        if (countBadge) {
          countBadge.textContent = pendingCount;
          countBadge.style.display = pendingCount > 0 ? 'inline-block' : 'none';
        }
      }

      this.departments = await ApiClient.getDepartments();
      this.employees = await ApiClient.getEmployees();

      const freshUser = this.employees.find(e => e.id === user.id);
      if (freshUser) {
        user.annualLeaveBalance = freshUser.annualLeaveBalance;
      }

      this.renderCurrentView();
    } catch (err) {
      console.error('Error refreshing data:', err);
      UIComponents.showToast('Veriler güncellenirken hata oluştu: ' + err.message, 'error');
    }
  }

  static renderCurrentView() {
    const user = AuthManager.getCurrentUser();

    // 1. Render Dashboard
    const statsContainer = document.getElementById('stats-grid-container');
    if (statsContainer) {
      statsContainer.innerHTML = UIComponents.renderStats(user, this.myLeaves);
    }
    const recentTable = document.getElementById('recent-leaves-tbody');
    if (recentTable) {
      recentTable.innerHTML = UIComponents.renderLeavesTable(this.myLeaves.slice(0, 5), user?.id);
    }

    // 2. Render Full My Leaves Table
    const fullTable = document.getElementById('my-leaves-tbody');
    if (fullTable) {
      fullTable.innerHTML = UIComponents.renderLeavesTable(this.myLeaves, user?.id);
    }

    // 3. Render HR Inbox
    const inboxGrid = document.getElementById('inbox-grid-container');
    if (inboxGrid) {
      inboxGrid.innerHTML = UIComponents.renderInboxList(this.companyLeaves);
    }

    // 4. Render Directory
    const dirContainer = document.getElementById('directory-container');
    if (dirContainer) {
      dirContainer.innerHTML = UIComponents.renderDepartments(this.departments, this.employees);
    }
  }

  // --- Actions ---

  static openCreateLeaveModal() {
    const modal = document.getElementById('modal-create-leave');
    const startInput = document.getElementById('leave-start-date');
    const endInput = document.getElementById('leave-end-date');

    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const inFourDays = new Date(tomorrow);
    inFourDays.setDate(inFourDays.getDate() + 4);

    startInput.value = tomorrow.toISOString().split('T')[0];
    endInput.value = inFourDays.toISOString().split('T')[0];

    modal.classList.add('active');
    startInput.dispatchEvent(new Event('change'));
  }

  static closeCreateLeaveModal() {
    document.getElementById('modal-create-leave').classList.remove('active');
  }

  static async handleCreateLeave() {
    const type = parseInt(document.getElementById('leave-type-select').value, 10);
    const startDate = document.getElementById('leave-start-date').value;
    const endDate = document.getElementById('leave-end-date').value;
    const reason = document.getElementById('leave-reason').value;

    try {
      await ApiClient.submitLeave({
        leaveType: type,
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString(),
        reason
      });

      this.closeCreateLeaveModal();
      UIComponents.showToast('İzin talebi başarıyla oluşturuldu.', 'success');
      await this.refreshAllData();
      this.switchTab('leaves');
    } catch (err) {
      UIComponents.showToast(err.message, 'error');
    }
  }

  static async cancelLeave(id) {
    if (!confirm(`#${id} numaralı izin talebini iptal etmek istediğinize emin misiniz?`)) return;

    try {
      await ApiClient.cancelLeave(id);
      UIComponents.showToast('İzin talebi iptal edildi.', 'info');
      await this.refreshAllData();
    } catch (err) {
      UIComponents.showToast('İptal işlemi başarısız: ' + err.message, 'error');
    }
  }

  static async approveLeave(id) {
    try {
      await ApiClient.approveLeave(id);
      UIComponents.showToast(`#${id} numaralı izin onaylandı ve bakiye güncellendi.`, 'success');
      await this.refreshAllData();
    } catch (err) {
      UIComponents.showToast('Onaylama hatası: ' + err.message, 'error');
    }
  }

  static openRejectModal(id, employeeName) {
    this.pendingRejectLeaveId = id;
    document.getElementById('reject-modal-title').textContent = `#${id} - ${employeeName} İzin Reddi`;
    document.getElementById('reject-reason-input').value = '';
    document.getElementById('modal-reject-leave').classList.add('active');
  }

  static closeRejectModal() {
    document.getElementById('modal-reject-leave').classList.remove('active');
    this.pendingRejectLeaveId = null;
  }

  static async handleConfirmReject() {
    const reason = document.getElementById('reject-reason-input').value.trim();
    if (!reason) {
      UIComponents.showToast('Lütfen geçerli bir red gerekçesi giriniz.', 'error');
      return;
    }

    try {
      await ApiClient.rejectLeave(this.pendingRejectLeaveId, reason);
      this.closeRejectModal();
      UIComponents.showToast(`#${this.pendingRejectLeaveId} numaralı talep reddedildi.`, 'info');
      await this.refreshAllData();
    } catch (err) {
      UIComponents.showToast('Reddetme hatası: ' + err.message, 'error');
    }
  }
}

window.App = App;
document.addEventListener('DOMContentLoaded', () => App.init());
