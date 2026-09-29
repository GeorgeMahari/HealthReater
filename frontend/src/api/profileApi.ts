import type { User } from "./authApi";
import { apiRequest } from "./http";

export const profileApi = {
  /** The profile data is optional; when sent, all four fields are validated (age 18–100). */
  update: (
    firstName: string,
    lastName: string,
    email: string,
    profileContext?: { sex: "Male" | "Female"; dateOfBirth: string; heightCm: number; weightKg: number }
  ) => apiRequest<User>("/api/profile", { method: "PUT", json: { firstName, lastName, email, ...profileContext } }),

  changePassword: (currentPassword: string, newPassword: string) =>
    apiRequest<void>("/api/profile/password", { method: "PUT", json: { currentPassword, newPassword } }),

  uploadAvatar: (image: Blob) => {
    const form = new FormData();
    form.append("file", image, "avatar");
    return apiRequest<User>("/api/profile/avatar", { method: "POST", form });
  },

  removeAvatar: () => apiRequest<User>("/api/profile/avatar", { method: "DELETE" }),

  deleteAccount: (password: string) => apiRequest<void>("/api/profile", { method: "DELETE", json: { password } }),
};
