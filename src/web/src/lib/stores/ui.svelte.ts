export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
  duration: number;
}

const DEFAULT_DURATION = 5000;

class UIStore {
  toasts = $state<Toast[]>([]);

  addToast(type: ToastType, message: string, duration = DEFAULT_DURATION) {
    const id = crypto.randomUUID();
    const toast: Toast = { id, type, message, duration };

    this.toasts = [...this.toasts, toast];

    if (duration > 0) {
      setTimeout(() => this.removeToast(id), duration);
    }

    return id;
  }

  removeToast(id: string) {
    this.toasts = this.toasts.filter((t) => t.id !== id);
  }

  success(message: string, duration?: number) {
    return this.addToast('success', message, duration);
  }

  error(message: string, duration?: number) {
    return this.addToast('error', message, duration);
  }

  warning(message: string, duration?: number) {
    return this.addToast('warning', message, duration);
  }

  info(message: string, duration?: number) {
    return this.addToast('info', message, duration);
  }
}

export const ui = new UIStore();

// Convenience functions for direct import
export const toast = {
  success: (message: string, duration?: number) => ui.success(message, duration),
  error: (message: string, duration?: number) => ui.error(message, duration),
  warning: (message: string, duration?: number) => ui.warning(message, duration),
  info: (message: string, duration?: number) => ui.info(message, duration)
};
