import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ToastModule],
  template: `
    <div class="shell">
      <aside class="shell__nav">
        <a routerLink="/" class="shell__brand">
          <span class="shell__mark">LV</span>
          <span class="shell__brand-text">
            <strong>LyricSongs</strong>
            <small>Lyric Video Generator</small>
          </span>
        </a>

        <nav class="shell__links">
          <a routerLink="/projects" routerLinkActive="is-active">Projects</a>
          <a routerLink="/new" routerLinkActive="is-active">New Project</a>
          <a routerLink="/editor" routerLinkActive="is-active">Editor</a>
          <a routerLink="/preview" routerLinkActive="is-active">Preview</a>
          <a routerLink="/render" routerLinkActive="is-active">Render</a>
          <a routerLink="/history" routerLinkActive="is-active">History</a>
        </nav>

        <p class="shell__footnote">1920×1080 · H.264 · AAC</p>
      </aside>

      <main class="shell__main">
        <router-outlet />
      </main>
    </div>
    <p-toast position="top-right" />
  `,
  styles: [
    `
      .shell {
        min-height: 100vh;
        display: grid;
        grid-template-columns: 15.5rem 1fr;
        background: var(--ls-bg);
      }

      .shell__nav {
        display: flex;
        flex-direction: column;
        gap: 2rem;
        padding: 1.5rem 1.1rem;
        border-right: 1px solid color-mix(in srgb, #fff 8%, transparent);
        background: linear-gradient(180deg, #121820 0%, #0c1016 100%);
      }

      .shell__brand {
        display: flex;
        gap: 0.75rem;
        align-items: center;
        text-decoration: none;
        color: inherit;
      }

      .shell__mark {
        width: 2.4rem;
        height: 2.4rem;
        display: grid;
        place-items: center;
        border-radius: 3px;
        background: var(--ls-accent);
        color: #1a1206;
        font-weight: 700;
        font-family: var(--ls-font-display);
      }

      .shell__brand-text {
        display: flex;
        flex-direction: column;
        line-height: 1.15;
      }

      .shell__brand-text strong {
        font-family: var(--ls-font-display);
        font-size: 1.15rem;
        font-weight: 600;
      }

      .shell__brand-text small {
        color: var(--ls-muted);
        font-size: 0.72rem;
        letter-spacing: 0.04em;
        text-transform: uppercase;
      }

      .shell__links {
        display: grid;
        gap: 0.35rem;
      }

      .shell__links a {
        text-decoration: none;
        color: var(--ls-muted);
        padding: 0.65rem 0.8rem;
        border-radius: 3px;
        font-size: 0.92rem;
        transition: background 0.15s ease, color 0.15s ease;
      }

      .shell__links a:hover,
      .shell__links a.is-active {
        color: var(--ls-text);
        background: color-mix(in srgb, var(--ls-accent) 14%, transparent);
      }

      .shell__footnote {
        margin-top: auto;
        font-size: 0.7rem;
        color: var(--ls-muted);
        letter-spacing: 0.06em;
      }

      .shell__main {
        min-width: 0;
        padding: 1.75rem 2rem 2.5rem;
      }

      @media (max-width: 900px) {
        .shell {
          grid-template-columns: 1fr;
        }

        .shell__nav {
          border-right: 0;
          border-bottom: 1px solid color-mix(in srgb, #fff 8%, transparent);
          gap: 1rem;
        }

        .shell__links {
          grid-auto-flow: column;
          grid-auto-columns: max-content;
          overflow-x: auto;
        }

        .shell__footnote {
          display: none;
        }

        .shell__main {
          padding: 1.25rem 1rem 2rem;
        }
      }
    `
  ]
})
export class ShellComponent {}
