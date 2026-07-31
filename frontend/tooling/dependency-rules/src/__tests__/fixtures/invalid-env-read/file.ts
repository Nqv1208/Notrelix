// Violation: direct process.env read inside package
export const apiUrl = process.env.VITE_API_URL;
