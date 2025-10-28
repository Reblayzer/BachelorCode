/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          DEFAULT: "#2c6bed",
          dark: "#224fc2",
          light: "#6e96ff",
        },
      },
    },
  },
  plugins: [],
};
