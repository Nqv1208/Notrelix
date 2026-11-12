/**
 * jsdom's Blob has no arrayBuffer() or text(); the mock backend reads an uploaded file with arrayBuffer(). The shim
 * reads through FileReader, which jsdom has.
 */
export function installBlobReaders(): void {
  const proto = Blob.prototype as Blob & {
    arrayBuffer?: () => Promise<ArrayBuffer>;
    text?: () => Promise<string>;
  };
  if (typeof proto.arrayBuffer !== "function") {
    proto.arrayBuffer = function (this: Blob) {
      return new Promise<ArrayBuffer>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as ArrayBuffer);
        reader.onerror = () => reject(reader.error);
        reader.readAsArrayBuffer(this);
      });
    };
  }
  if (typeof proto.text !== "function") {
    proto.text = function (this: Blob) {
      return new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result));
        reader.onerror = () => reject(reader.error);
        reader.readAsText(this);
      });
    };
  }
}
