import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'new' },
      {
        path: 'projects',
        loadComponent: () =>
          import('./features/projects/projects.component').then((m) => m.ProjectsComponent)
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./features/new-project/new-project.component').then((m) => m.NewProjectComponent)
      },
      {
        path: 'editor',
        loadComponent: () =>
          import('./features/editor/editor.component').then((m) => m.EditorComponent)
      },
      {
        path: 'preview',
        loadComponent: () =>
          import('./features/preview/preview.component').then((m) => m.PreviewComponent)
      },
      {
        path: 'render',
        loadComponent: () =>
          import('./features/render/render.component').then((m) => m.RenderComponent)
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./features/history/history.component').then((m) => m.HistoryComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'new' }
];
