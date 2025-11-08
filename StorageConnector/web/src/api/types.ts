export type ProviderType = "Google" | "Microsoft";

export type ConnectionStatus = {
  provider: ProviderType;
  isLinked: boolean;
  scopes: string[];
};

export type RegisterRequest = {
  email: string;
  password: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type LoginResponse = {
  token: string;
};

export type RegisterResponse = {
  message: string;
};

export type RedirectResponse = {
  redirectUrl: string;
};

export type CurrentUserResponse = {
  email: string;
};
