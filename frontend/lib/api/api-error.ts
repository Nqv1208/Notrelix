
export class ApiError extends Error{
    status: number
    data?: unknown

    constructor(status: number, message: string, data?: unknown){
        super(message)
        this.status = status
        this.data = data
    }
}