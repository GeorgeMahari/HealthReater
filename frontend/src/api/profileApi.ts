import type { User } from "./authApi";
import { apiRequest } from "./http";

export const profileApi = {
  update: (firstName: string, lastName: string, email: string) =>
    apiRequest<User>("/api/profile", { method: "PUT", json: { firstName, lastName, email } }),

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
