# Capacitor Wrapper Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a Capacitor wrapper so the SvelteKit PWA can ship to App Store/Google Play.

**Architecture:** Keep the existing SvelteKit app as the single source of UI truth. Build static assets for a Capacitor web view and sync them into iOS/Android projects. Use Capacitor plugins for native features (push, camera) later.

**Tech Stack:** SvelteKit, Capacitor, TypeScript, iOS/Android native shells.

### Task 1: Add Capacitor dependencies and config

**Files:**
- Modify: `src/web/package.json`
- Create: `src/web/capacitor.config.ts`

**Step 1: Add npm scripts for mobile build**

Add scripts:
- `build:mobile` (build static output for Capacitor)
- `cap:sync` (sync web assets to native shells)

**Step 2: Install Capacitor packages**

Run: `npm install @capacitor/core @capacitor/cli`
Expected: packages added to `src/web/package.json`.

**Step 3: Create Capacitor config**

Create `src/web/capacitor.config.ts` with app id, app name, and `webDir` that matches the SvelteKit build output.

**Step 4: Verify config loads**

Run: `npx cap doctor`
Expected: Capacitor CLI prints config info without errors.

### Task 2: Ensure static build output for Capacitor

**Files:**
- Modify: `src/web/svelte.config.js`

**Step 1: Add adapter-static for mobile build**

Switch to `@sveltejs/adapter-static` or add a conditional so `build:mobile` outputs a static site.

**Step 2: Run mobile build**

Run: `npm run build:mobile`
Expected: static output directory exists and matches `webDir` in config.

### Task 3: Add native platforms and sync

**Files:**
- Create: `src/web/android/*`
- Create: `src/web/ios/*`

**Step 1: Add platform packages**

Run: `npm install @capacitor/android @capacitor/ios`

**Step 2: Add platforms**

Run: `npx cap add android` and `npx cap add ios`
Expected: native project directories created.

**Step 3: Sync web assets**

Run: `npx cap sync`
Expected: web assets copied into native projects without errors.

**Step 4: Manual smoke test**

Run: `npx cap open android` and/or `npx cap open ios` and confirm app boots to the offline-first shell.

### Task 4: Document usage and CI hooks

**Files:**
- Modify: `docs/05-features/pwa.md`
- Modify: `docs/IMPLEMENTATION_TASKS.md`

**Step 1: Document local workflow**

Add short steps for `build:mobile`, `cap:sync`, and opening native projects.

**Step 2: Note CI considerations**

Add a checklist item for mobile builds (without implementing CI yet).
