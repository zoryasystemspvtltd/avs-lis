import { UserAccess } from './useraccess';

export class AuthenticationToken {
    id: string;
    userName: string;
    password: string;
    firstName: string;
    lastName: string;
    accessToken: string;
    emailConfirmed: string;
    expires: Date;
    issued: Date;
    token_type: string;
    refreshToken: string;
    roles: string[];
    access: UserAccess[];
    displayName: string;
}

export class User{
    id: string;
    userName: string;
    password: string;
    firstName: string;
    lastName: string;
    displayName: string;
}

export class UserInfo {
    first_name: string;
    last_name: string;
    email: string;
    phone_number: string;
    alternative_email: string;
    dob: string;
    area_of_interest: string;
    qualification: string;
    country: string;
    state: string;
    address: string;
    zip: string;
}


export class ChangePassword {
    oldPassword: string;
    newPassword: string;
    confirmPassword: string;
}

export class ResetPassword {
    email: string;
    password: string;
    code: string;
}

export class KeyValuePair{
    key:string;
    value:string;
}
