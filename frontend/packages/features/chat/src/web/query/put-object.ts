/**
 * The byte leg of a direct upload (UC-CHT-G08, two-step): the client PUTs the file to the pre-signed URL the server issued. It is sent
 * with XMLHttpRequest because only XHR reports upload progress; the URL is a signature for its window, so no session header is sent and
 * the request must not be retried after it expires. A failure carries the HTTP status (0 for a network failure) so the upload is classified
 * like every other failed upload.
 */

export interface ObjectPutProgress {
  readonly loaded: number;
  readonly total: number;
}

export interface ObjectPutInput {
  readonly url: string;
  readonly method: string;
  readonly contentType: string;
  readonly body: Blob;
  readonly onProgress?: (progress: ObjectPutProgress) => void;
}

export class ObjectPutError extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`The file upload failed (status ${status}).`);
    this.name = "ObjectPutError";
    this.status = status;
  }
}

export function putObjectWithProgress(input: ObjectPutInput): Promise<void> {
  return new Promise<void>((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open(input.method, input.url, true);
    xhr.setRequestHeader("Content-Type", input.contentType);
    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        input.onProgress?.({ loaded: event.loaded, total: event.total });
      }
    };
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
      } else {
        reject(new ObjectPutError(xhr.status));
      }
    };
    xhr.onerror = () => reject(new ObjectPutError(0));
    xhr.onabort = () => reject(new ObjectPutError(0));
    xhr.send(input.body);
  });
}
