import { HttpClient, HttpEvent, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '/environments/environment';

@Injectable({
    providedIn: 'root'
})
export class GenericApiService {

    private readonly baseUrl = environment.baseUrl;

    constructor(
        private readonly _http: HttpClient
    ) { }

    /** Generic Http get request */
    get<T>(url: string, data: any | null | undefined = null, isShowLoader: boolean = true, customHeaders?: HttpHeaders): Observable<T> {
        let httpParams: HttpParams = new HttpParams();

        if (data) {
            Object.keys(data)
                .forEach(x => {
                    httpParams = httpParams.append(x, data[x]);
                });
        }

        const options: { params: HttpParams, headers?: HttpHeaders } = { params: httpParams };

        if (!isShowLoader) {
            options.headers = this.getNoShowHeaders();
        } else if (customHeaders) {
            options.headers = customHeaders;
        }

        if (!isShowLoader && customHeaders) {
            const noShowHeaders = this.getNoShowHeaders();
            customHeaders.keys().forEach(key => {
                options.headers = options.headers!.set(key, customHeaders.get(key) || '');
            });
        }

        const fullUrl = `${this.baseUrl}/${url}`;
        return this._http.get<T>(fullUrl, options);
    }

    /** Generic Http delete request */
    delete<T>(url: string, isShowLoader: boolean = true): Observable<T> {
        const fullUrl = `${this.baseUrl}/${url}`;

        const options: { headers?: HttpHeaders } = {};

        if (!isShowLoader)
            options.headers = this.getNoShowHeaders();

        return this._http.delete<T>(fullUrl, options);
    }

    /** Generic Http post request */
    post<T>(url: string, body: any, isShowLoader: boolean = true): Observable<T> {
        const fullUrl = `${this.baseUrl}/${url}`;

        const options: { headers?: HttpHeaders } = {};

        if (!isShowLoader)
            options.headers = this.getNoShowHeaders();

        return this._http.post<T>(fullUrl, body, options);
    }

    postAi<T>(url: string, body: any, isShowLoader: boolean = true): Observable<T> {
        const fullUrl = `${this.baseUrl}/${url}`;
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        if (!isShowLoader) {
            headers = headers.set('No-Show-Loader', 'true');
        }
        const options = { headers };
        return this._http.post<T>(fullUrl, body, options);
    }

    getBlobByUsingPost(url: string, body: any, isShowLoader: boolean = true): Observable<HttpResponse<Blob>> {
        const fullUrl = `${this.baseUrl}/${url}`;

        const options: { observe: 'response', responseType: 'blob', headers?: HttpHeaders }
            = { observe: 'response', responseType: 'blob' };

        if (!isShowLoader)
            options.headers = this.getNoShowHeaders();

        return this._http.post(fullUrl, body, options);
    }

    /** Generic Http get request for blob response */
    getBlob(url: string, isShowLoader: boolean = true): Observable<Blob> {
        const fullUrl = `${this.baseUrl}/${url}`;

        const options: { responseType: 'blob', headers?: HttpHeaders } = { responseType: 'blob' };

        if (!isShowLoader)
            options.headers = this.getNoShowHeaders();

        return this._http.get(fullUrl, options);
    }

    /** Generic Http put request */
    put<T>(url: string, body: any, isShowLoader: boolean = true): Observable<T> {
        const fullUrl = `${this.baseUrl}/${url}`;

        if (!isShowLoader)
            return this._http.put<T>(fullUrl, body, { headers: this.getNoShowHeaders() });

        return this._http.put<T>(fullUrl, body);
    }

    /** Generic Http patch request */
    patch<T>(url: string, body: any, isShowLoader: boolean = true): Observable<T> {
        const fullUrl = `${this.baseUrl}/${url}`;

        if (!isShowLoader)
            return this._http.patch<T>(fullUrl, body, { headers: this.getNoShowHeaders() });

        return this._http.patch<T>(fullUrl, body);
    }

    uploadFile<T>(url: string, formData: FormData, isShowLoader: boolean = true): Observable<HttpEvent<T>> {
        const fullUrl = `${this.baseUrl}/${url}`;

        if (!isShowLoader) {
            let headers = this.getNoShowHeaders();
            headers = headers.set('Content-Type', 'multipart/form-data');

            return this._http.post<T>(fullUrl, formData, {
                reportProgress: true,
                observe: 'events',
                responseType: 'json',
                headers: headers,
            });
        }

        let headers: HttpHeaders = new HttpHeaders();
        headers = headers.set('Content-Type', 'multipart/form-data');

        return this._http.post<T>(fullUrl, formData, {
            reportProgress: true,
            observe: 'events',
            responseType: 'json',
            headers: headers,
        });
    }

    private getNoShowHeaders() {
        return (new HttpHeaders()).set('X-No-Loader', 'true');
    }
}
