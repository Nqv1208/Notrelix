export type User = {
    id: string,
    email: string,
    name: string,
    avatarUrl: string
}

export type LoginRequest = {
    email: string,
    password: string
}

export type LoginResponse = {
    accessToken: string,
    refreshToken: string,
    expriresAt: string,
    user: User
}

export type RegisterRequest = {
    email: string,
    password: string,
    name: string
}

export type RegisterResponse  = Required<LoginResponse>

export type LogoutRequest = Pick<LoginResponse, "refreshToken">

export type RefreshRequest = Pick<LoginResponse, "refreshToken">