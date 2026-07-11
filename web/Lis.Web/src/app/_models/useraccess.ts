export class UserAccess {
    id: string;
    name: string;
    url: string;
    access: number;
}

/** Menu-level overlay from RoleMenuPermission. Empty list = fall back to module permissions. */
export class MenuAccess {
    menuKey: string;
    moduleId: number;
    moduleName: string;
    access: number;
}
