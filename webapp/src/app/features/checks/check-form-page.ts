import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ChecksApi } from '../../core/api/checks-api';
import { describeError } from '../../core/api/describe-error';
import { CheckInput } from '../../core/api/models';
import { ChecksStore } from '../../core/state/checks.store';

/** Matches the server's limits (Check.MinInterval and friends), so most mistakes are caught before a round trip. */
export const INTERVAL_OPTIONS = [
  { seconds: 30, label: 'Every 30 seconds' },
  { seconds: 60, label: 'Every minute' },
  { seconds: 300, label: 'Every 5 minutes' },
  { seconds: 900, label: 'Every 15 minutes' },
  { seconds: 1800, label: 'Every 30 minutes' },
  { seconds: 3600, label: 'Every hour' },
] as const;

@Component({
  selector: 'app-check-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './check-form-page.html',
})
export class CheckFormPage implements OnInit {
  private readonly api = inject(ChecksApi);
  private readonly store = inject(ChecksStore);
  private readonly router = inject(Router);

  /** Route parameter, bound by withComponentInputBinding. Absent when creating. */
  readonly id = input<string>();

  protected readonly intervals = INTERVAL_OPTIONS;
  protected readonly saving = signal(false);
  protected readonly loading = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    url: new FormControl('https://', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(2048), Validators.pattern(/^https?:\/\/[^\s/$.?#].[^\s]*$/i)],
    }),
    intervalSeconds: new FormControl(60, { nonNullable: true, validators: [Validators.required] }),
    failureThreshold: new FormControl(3, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(10)],
    }),
  });

  async ngOnInit(): Promise<void> {
    const id = this.id();
    if (!id) {
      return;
    }

    this.loading.set(true);
    try {
      const check = this.store.find(id) ?? (await firstValueFrom(this.api.get(id)));
      this.form.setValue({
        name: check.name,
        url: check.url,
        intervalSeconds: check.intervalSeconds,
        failureThreshold: check.failureThreshold,
      });
    } catch (error) {
      this.serverError.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected showError(control: keyof typeof this.form.controls): boolean {
    const field = this.form.controls[control];
    return field.invalid && (field.touched || field.dirty);
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const input: CheckInput = this.form.getRawValue();
    const id = this.id();

    this.saving.set(true);
    this.serverError.set(null);
    try {
      const saved = id ? await this.store.update(id, input) : await this.store.create(input);
      await this.router.navigate(['/checks', saved.id]);
    } catch (error) {
      // The server is the source of truth for validation; show its message as-is.
      this.serverError.set(describeError(error));
    } finally {
      this.saving.set(false);
    }
  }
}
