import { ChangeDetectionStrategy, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { sendFile, SendFileRequest } from '../..';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-file-send',
  templateUrl: './file-send.component.html',
  styleUrl: './file-send.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileSendComponent implements OnInit {
  @ViewChild('fileInputRef') fileInputRef!: ElementRef;
  formGroup: FormGroup = null!;
  fileError: string | null = null;

  get emailInput() { return this.formGroup.get('email') as FormControl; }
  get fileInput() { return this.formGroup.get('file') as FormControl; }

  constructor(private readonly store: Store) { }

  ngOnInit(): void {
    this.formGroup = new FormGroup({
      email: new FormControl("", [Validators.required, Validators.email, Validators.maxLength(256)]),
      file: new FormControl(null, [Validators.required])
    });
  }

  onFileSelected(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement.files && inputElement.files.length > 0) {

      const file = inputElement.files[0];

      if (file.size > environment.maxFileSize) { // 1MB validation
        this.fileError = `File size must be ${environment.maxFileSize / (1024 * 1024)}MB or less.`;
        this.fileInput.setValue(null);
        return;
      }

      this.fileInput.setValue(file);
      this.fileInput.markAsTouched();
    }
  }

  sendFile() {
    if (this.formGroup.valid) {
      const req: SendFileRequest = {
        email: this.emailInput.value,
        file: this.fileInput.value,
      };
      this.store.dispatch(sendFile({ req: req }));
      this.formGroup.reset();
      this.fileInputRef.nativeElement.value = "";
    } else {
      this.formGroup.markAllAsTouched();
      this.fileError = this.fileInput.value ? null : "Please select a file.";
    }
  }
}
