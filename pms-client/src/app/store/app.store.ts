import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';

interface AppState {
  sidebarCollapsed: boolean;
  activePropertyId: string | null;
}

const initialState: AppState = {
  sidebarCollapsed: false,
  activePropertyId: null
};

export const AppStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store) => ({
    toggleSidebar() {
      patchState(store, { sidebarCollapsed: !store.sidebarCollapsed() });
    },
    setSidebarCollapsed(collapsed: boolean) {
      patchState(store, { sidebarCollapsed: collapsed });
    },
    setActivePropertyId(id: string | null) {
      patchState(store, { activePropertyId: id });
    }
  }))
);
