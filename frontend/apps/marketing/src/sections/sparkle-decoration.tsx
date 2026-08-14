import * as React from "react";

export function SparkleDecoration({
  className,
  ...props
}: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="25"
      height="26"
      viewBox="0 0 25 26"
      fill="none"
      className={className}
      {...props}
    >
      <path
        fill="currentColor"
        fillRule="evenodd"
        d="M8.44 24.263c-2.266-.636-4.638-.957-6.94-1.25-.495-.062-.956.282-.991.766a.88.88 0 0 0 .744.99c2.23.283 4.532.585 6.691 1.197a.9.9 0 0 0 1.098-.61.88.88 0 0 0-.602-1.093m6.869-9.015c-3.647-3.722-7.754-6.964-11.33-10.786a.87.87 0 0 0-1.24-.043.87.87 0 0 0-.035 1.252c3.576 3.832 7.683 7.085 11.33 10.817a.913.913 0 0 0 1.275.011c.318-.342.354-.903 0-1.251M22.143.933l.318 6.39c0 .489.425.865.92.842.497-.024.85-.44.85-.928l-.318-6.4A.91.91 0 0 0 22.957 0a.88.88 0 0 0-.814.932Z"
        clipRule="evenodd"
      />
    </svg>
  );
}
