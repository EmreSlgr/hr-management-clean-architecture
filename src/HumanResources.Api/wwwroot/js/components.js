/**
 * UI Component Templates & Rendering Engine
 * Stripe & GitHub Enterprise High-Contrast Design System
 */

class UIComponents {
  static formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('tr-TR', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  static getStatusBadge(status) {
    const s = (status || '').toLowerCase();
    switch (s) {
      case 'approved':
        return `<span class="status-pill status-approved"><span class="status-dot"></span>Onaylandı</span>`;
      case 'rejected':
        return `<span class="status-pill status-rejected"><span class="status-dot"></span>Reddedildi</span>`;
      case 'cancelled':
        return `<span class="status-pill status-cancelled"><span class="status-dot"></span>İptal Edildi</span>`;
      case 'pending':
      default:
        return `<span class="status-pill status-pending"><span class="status-dot"></span>Onay Bekliyor</span>`;
    }
  }

  static getLeaveTypeBadge(type) {
    const typeMap = {
      'Annual': 'Yıllık İzin',
      'Sick': 'Hastalık İzni',
      'Maternity': 'Doğum İzni',
      'Unpaid': 'Ücretsiz İzin'
    };
    return `<span class="type-pill">${typeMap[type] || type}</span>`;
  }

  static renderStats(currentUser, myLeaves = []) {
    const balance = currentUser?.annualLeaveBalance ?? 15;
    const pendingCount = myLeaves.filter(l => l.status === 'Pending').length;
    const approvedDays = myLeaves
      .filter(l => l.status === 'Approved' && l.leaveType === 'Annual')
      .reduce((sum, l) => sum + (l.totalDays || 0), 0);

    return `
      <div class="metric-card">
        <div class="metric-label">Kalan Yıllık İzin</div>
        <div class="metric-value">
          ${balance} <span class="metric-unit">gün</span>
        </div>
        <div class="metric-caption">Yasal hak ediş bakiyesi</div>
      </div>

      <div class="metric-card">
        <div class="metric-label">Kullanılan İzin</div>
        <div class="metric-value">
          ${approvedDays} <span class="metric-unit">gün</span>
        </div>
        <div class="metric-caption">Onaylanan yıllık izinler toplamı</div>
      </div>

      <div class="metric-card">
        <div class="metric-label">Onay Bekleyen</div>
        <div class="metric-value">
          ${pendingCount} <span class="metric-unit">talep</span>
        </div>
        <div class="metric-caption">İnceleme aşamasındaki başvurular</div>
      </div>

      <div class="metric-card">
        <div class="metric-label">Yetki Seviyesi</div>
        <div class="metric-value" style="font-size: 1.35rem; color: var(--text-primary);">
          ${currentUser?.role || 'Employee'}
        </div>
        <div class="metric-caption">${currentUser?.roleLabel || 'Kurumsal Kullanıcı'}</div>
      </div>
    `;
  }

  static renderLeavesTable(leaves, currentUserId) {
    if (!leaves || leaves.length === 0) {
      return `
        <tr>
          <td colspan="7" class="empty-state">
            Kayıtlı herhangi bir izin başvurusu bulunmamaktadır.
          </td>
        </tr>
      `;
    }

    return leaves.map(leave => {
      const isPending = leave.status === 'Pending';
      const isOwner = leave.employeeId === currentUserId;

      const actionButton = (isPending && isOwner)
        ? `<button class="btn btn-secondary btn-sm" onclick="App.cancelLeave(${leave.id})">İptal Et</button>`
        : `<span style="color: var(--text-subtle);">-</span>`;

      return `
        <tr>
          <td style="font-family: var(--font-mono); font-size: 0.78rem; color: var(--text-muted);">#${leave.id}</td>
          <td>${this.getLeaveTypeBadge(leave.leaveType)}</td>
          <td>
            <span style="font-weight: 600; color: var(--text-primary);">${this.formatDate(leave.startDate)}</span>
            <span style="color: var(--text-subtle); margin: 0 0.25rem;">—</span>
            <span style="font-weight: 600; color: var(--text-primary);">${this.formatDate(leave.endDate)}</span>
          </td>
          <td style="font-weight: 600; font-variant-numeric: tabular-nums;">${leave.totalDays} gün</td>
          <td>${this.getStatusBadge(leave.status)}</td>
          <td>
            <div style="max-width: 280px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: var(--text-secondary);" title="${leave.reason}">
              ${leave.reason}
            </div>
            ${leave.rejectionReason ? `<div style="font-size: 0.75rem; color: #b91c1c; margin-top: 0.2rem;">Gerekçe: ${leave.rejectionReason}</div>` : ''}
          </td>
          <td style="text-align: right;">${actionButton}</td>
        </tr>
      `;
    }).join('');
  }

  static renderInboxList(leaves) {
    const pendingLeaves = (leaves || []).filter(l => l.status === 'Pending');

    if (pendingLeaves.length === 0) {
      return `
        <div class="panel" style="padding: 2.5rem; text-align: center; color: var(--text-muted);">
          Onay bekleyen izin talebi bulunmamaktadır. Tüm başvurular değerlendirilmiştir.
        </div>
      `;
    }

    return pendingLeaves.map(leave => {
      const initials = (leave.employeeName || 'Personel')
        .split(' ')
        .map(n => n[0])
        .slice(0, 2)
        .join('')
        .toUpperCase();

      return `
        <div class="inbox-item" id="inbox-card-${leave.id}">
          <div class="inbox-item-left">
            <div class="inbox-avatar">${initials}</div>
            <div class="inbox-item-meta">
              <div class="inbox-item-name">
                ${leave.employeeName}
                ${this.getLeaveTypeBadge(leave.leaveType)}
              </div>
              <div class="inbox-item-sub">
                Talep No: #${leave.id} • ${this.formatDate(leave.startDate)} — ${this.formatDate(leave.endDate)} (${leave.totalDays} gün)
              </div>
              <div class="inbox-item-reason">
                "${leave.reason}"
              </div>
            </div>
          </div>

          <div class="inbox-item-right">
            <button class="btn btn-approve btn-sm" onclick="App.approveLeave(${leave.id})">
              Onayla
            </button>
            <button class="btn btn-danger btn-sm" onclick="App.openRejectModal(${leave.id}, '${leave.employeeName.replace(/'/g, "\\'")}')">
              Reddet
            </button>
          </div>
        </div>
      `;
    }).join('');
  }

  static renderDepartments(departments, employees) {
    if (!departments || departments.length === 0) return '';

    return departments.map(d => {
      const deptEmployees = (employees || []).filter(e => e.departmentId === d.id);

      return `
        <div class="panel">
          <div class="panel-header">
            <div>
              <span class="panel-title">${d.name}</span>
              <p style="font-size: 0.78rem; color: var(--text-muted); margin-top: 0.15rem;">${d.description}</p>
            </div>
            <span class="type-pill">${deptEmployees.length} Personel</span>
          </div>

          <div class="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Personel Adı</th>
                  <th>E-Posta Adresi</th>
                  <th>Görevi / Rolü</th>
                  <th style="width: 130px; text-align: right;">Kalan Yıllık İzin</th>
                </tr>
              </thead>
              <tbody>
                ${deptEmployees.map(emp => `
                  <tr>
                    <td style="font-weight: 600; color: var(--text-primary);">${emp.fullName}</td>
                    <td>${emp.email}</td>
                    <td>
                      <span class="role-tag ${emp.role.toLowerCase()}">${emp.role}</span>
                      <span style="color: var(--text-secondary); margin-left: 0.35rem;">${emp.jobTitle}</span>
                    </td>
                    <td style="font-weight: 600; font-variant-numeric: tabular-nums; text-align: right;">${emp.annualLeaveBalance} gün</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      `;
    }).join('');
  }

  static showToast(message, type = 'success') {
    let container = document.getElementById('toast-container');
    if (!container) {
      container = document.createElement('div');
      container.id = 'toast-container';
      container.className = 'toast-container';
      document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.textContent = message;

    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transition = 'opacity 0.15s ease';
      setTimeout(() => toast.remove(), 150);
    }, 3200);
  }
}

window.UIComponents = UIComponents;
