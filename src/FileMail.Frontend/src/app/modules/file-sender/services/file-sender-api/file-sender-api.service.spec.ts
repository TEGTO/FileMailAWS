import { TestBed } from '@angular/core/testing';

import { FileSenderApiService } from './file-sender-api.service';

describe('FileSenderApiService', () => {
  let service: FileSenderApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(FileSenderApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
