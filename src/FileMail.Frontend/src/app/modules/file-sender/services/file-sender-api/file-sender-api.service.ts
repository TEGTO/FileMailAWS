import { HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable } from 'rxjs';
import { SendFileRequest } from '../..';
import { BaseApiService } from '../../../shared';

@Injectable({
  providedIn: 'root'
})
export class FileSenderApiService extends BaseApiService {
  sendFile(req: SendFileRequest): Observable<HttpResponse<void>> {
    const formData = new FormData();
    formData.append("Email", req.email);
    formData.append("File", req.file);

    return this.httpClient.post<void>(
      this.combinePathWithFileApiUrl(``),
      formData,
      { observe: 'response' }
    ).pipe(
      catchError((resp) => this.handleError(resp))
    );
  }

  private combinePathWithFileApiUrl(subpath: string): string {
    return this.urlDefiner.combineWithApiUrl("/file" + subpath);
  }
}
