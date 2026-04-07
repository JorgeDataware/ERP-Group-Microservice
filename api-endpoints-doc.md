# GroupsMicroservice - Documentación de API

## Información del Servicio

**Nombre:** GroupsMicroservice

**Descripción:** Microservicio para gestión de grupos y membresías. Permite crear, editar, desactivar grupos, así como agregar y eliminar miembros. Utiliza autenticación JWT con claims de permisos.

**Rutas Internas Base:**
- `/api/Group`

**Notas Importantes:**
- Todos los endpoints devuelven el formato `StandardResponse<T>` con `statusCode`, `intOpCode`, `message` y `data` (siempre array)
- La autenticación se realiza mediante JWT bearer token o cookie 'jwt'
- Los permisos se validan desde claims del JWT con type 'permission'
- El userId se obtiene del claim NameIdentifier del JWT
- Solo el owner del grupo puede agregar/editar/eliminar miembros (excepto SuperAdmin para deactivate)
- Los grupos con Status.Inactive no permiten operaciones de modificación
- SuperAdminId especial: `00000000-0000-0000-0000-000000000001`

---

## Enums Globales

### Status
Estado de las entidades (grupos y usuarios)

| Valor | Numérico | Descripción |
|-------|----------|-------------|
| Active | 1 | Entidad activa |
| Inactive | 2 | Entidad inactiva |

---

## Permisos Globales

| Permiso | Constante | Descripción |
|---------|-----------|-------------|
| `canCreate_Groups` | GroupPermissions.CanCreate | Permite crear nuevos grupos. Requerido en: AddGroup |
| `canRead_Groups` | GroupPermissions.CanRead | Permite leer/consultar grupos y miembros. Requerido en: GetGroups, GetGroupById, GetGroupMembers |
| `canUpdate_Groups` | GroupPermissions.CanUpdate | Permite actualizar grupos y gestionar miembros. Requerido en: AddMember, EditGroup, RemoveMember |
| `canDelete_Groups` | GroupPermissions.CanDelete | Permite desactivar grupos. Requerido en: DeactivateGroup |

---

## Tabla Resumen de Endpoints

| Method | Gateway Endpoint | Internal Endpoint | Auth | Permissions | Summary |
|--------|-----------------|-------------------|------|-------------|---------|
| POST | `/groups/` | `/api/Group/AddGroup` | ✓ | canCreate_Groups | Crear un nuevo grupo |
| POST | `/groups/members` | `/api/Group/AddMember` | ✓ | canUpdate_Groups | Agregar un miembro a un grupo |
| GET | `/groups/` | `/api/Group/GetGroups` | ✓ | canRead_Groups | Obtener lista de todos los grupos |
| PATCH | `/groups/{groupId}` | `/api/Group/EditGroup/{groupId}` | ✓ | canUpdate_Groups | Editar nombre y descripción de un grupo |
| GET | `/groups/{groupId}` | `/api/Group/GetGroupById/{groupId}` | ✓ | canRead_Groups | Obtener detalles completos de un grupo |
| GET | `/groups/{groupId}/members` | `/api/Group/GetGroupMembers/{groupId}` | ✓ | canRead_Groups | Obtener lista de miembros de un grupo |
| DELETE | `/groups/{groupId}/members` | `/api/Group/RemoveMember` | ✓ | canUpdate_Groups | Eliminar un miembro de un grupo |
| PATCH | `/groups/{groupId}/deactivate` | `/api/Group/{groupId}/deactivate` | ✓ | canDelete_Groups | Desactivar un grupo |

---

## Endpoints Detallados

### 1. POST - Crear un nuevo grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/`  
**Endpoint Interno:** `POST /api/Group/AddGroup`

Permite a un usuario autenticado con permiso de creación crear un nuevo grupo del cual será el owner.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canCreate_Groups`
- **Reglas de ownership:**
  - El usuario autenticado se convierte automáticamente en owner del grupo (CreatedByUserId)
  - No se requiere ser owner de nada previo, solo tener el permiso

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido
  - Ejemplo: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`

**Cookies (alternativo):**
- `jwt`: Token JWT si no se envía header Authorization

**Body:**
```json
{
  "Name": "Equipo de Desarrollo",
  "Description": "Grupo para desarrolladores del proyecto X"
}
```

**Campos requeridos:** Name, Description

#### Responses

##### 201 - SGRCR201
**Cuándo ocurre:** Cuando el grupo se crea exitosamente

```json
{
  "statusCode": 201,
  "intOpCode": "SGRCR201",
  "message": "Group created successfully.",
  "data": []
}
```

##### 401 - EGRAU401
**Cuándo ocurre:** Cuando no se puede obtener el userId del JWT (claim NameIdentifier inválido o ausente)

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "User authentication is required to add a group.",
  "data": []
}
```

##### 403 - EGRFB403
**Cuándo ocurre:** Cuando el usuario autenticado no tiene el permiso 'canCreate_Groups' en sus claims

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el userId del JWT no existe en la tabla 'user'

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified user was not found.",
  "data": []
}
```

##### 409 - EGRCF409
**Cuándo ocurre:** Cuando ya existe un grupo con el mismo Name

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "A group with the specified name already exists.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Cuando ocurre un error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while adding the group.",
  "data": []
}
```

#### Lógica de Negocio
- El nombre del grupo debe ser único en todo el sistema
- El usuario que crea el grupo se convierte automáticamente en el owner (CreatedByUserId)
- El grupo se crea con Status.Active por defecto
- El userId del JWT debe corresponder a un usuario existente en la base de datos

#### Dependencias
- **Repositorios:** IGroupRepositorie.AddGroupAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Helpers:** AutoMapper (para mapear AddGroupRequest a Group entity)
- **Entidades:** Group (tabla: group), User (tabla: user)
- **Claims JWT:** NameIdentifier (userId), permission (permisos)

#### Notas para Frontend
- El endpoint NO retorna el ID del grupo creado en data (está vacío), pero el repositorio sí lo retorna internamente
- Validar que Name y Description no estén vacíos antes de enviar
- Mostrar mensaje específico si el nombre ya existe (409)
- Después de crear exitosamente, refrescar la lista de grupos
- El usuario que crea el grupo puede gestionarlo sin restricciones (es el owner)

---

### 2. POST - Agregar un miembro a un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/members`  
**Endpoint Interno:** `POST /api/Group/AddMember`

Permite al owner de un grupo agregar un nuevo miembro. Requiere que el grupo esté activo y que el usuario a agregar exista.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canUpdate_Groups`
- **Reglas de ownership:**
  - Solo el owner del grupo (CreatedByUserId) puede agregar miembros
  - Si el requesterId != owner, retorna 403 con mensaje específico
  - SuperAdmin NO tiene privilegios especiales para este endpoint

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

**Body:**
```json
{
  "GroupId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "memberId": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
}
```

**Campos requeridos:** GroupId, memberId

**Validaciones:**
- GroupId debe existir y tener Status.Active
- memberId debe corresponder a un usuario existente
- El miembro no debe estar ya en el grupo (validación de duplicado)
- El requesterId (del JWT) debe ser el owner del grupo

#### Responses

##### 200 - SGRMB200
**Cuándo ocurre:** Cuando el miembro se agrega exitosamente al grupo

```json
{
  "statusCode": 200,
  "intOpCode": "SGRMB200",
  "message": "Group member added successfully.",
  "data": []
}
```

##### 401 - EGRAU401
**Cuándo ocurre:** Cuando no se puede obtener el userId del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403 (Sin permiso)
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canUpdate_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 403 - EGRFB403 (No es owner)
**Cuándo ocurre:** Cuando el requesterId no es el owner del grupo

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can add members to the group.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el memberId no corresponde a un usuario existente

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified user was not found.",
  "data": []
}
```

##### 409 - EGRCF409 (Grupo inactivo)
**Cuándo ocurre:** Cuando el grupo no existe o su Status != Active

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The specified group was not found or is inactive.",
  "data": []
}
```

##### 409 - EGRCF409 (Miembro duplicado)
**Cuándo ocurre:** Cuando el memberId ya está en la tabla group_members para ese GroupId

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The specified member is already part of the group.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while adding the member to the group.",
  "data": []
}
```

#### Lógica de Negocio
- Solo el owner del grupo puede agregar miembros
- El grupo debe estar activo (Status.Active)
- El usuario a agregar debe existir en la base de datos
- No se puede agregar un miembro duplicado
- Se crea un registro en group_members con un nuevo Guid como Id

#### Dependencias
- **Repositorios:** IGroupRepositorie.AddMemberAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Entidades:** Group (tabla: group), User (tabla: user), GroupMembers (tabla: group_members)
- **Claims JWT:** NameIdentifier (requesterId), permission

#### Notas para Frontend
- Validar que ambos GUIDs sean válidos antes de enviar
- Mostrar mensaje específico para cada tipo de error (409 puede ser por grupo inactivo O miembro duplicado)
- Después de agregar exitosamente, refrescar la lista de miembros del grupo
- Solo mostrar opción de agregar miembros si el usuario actual es el owner del grupo
- Considerar deshabilitar el botón si el grupo está inactivo

---

### 3. GET - Obtener lista de todos los grupos

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/`  
**Endpoint Interno:** `GET /api/Group/GetGroups`

Retorna todos los grupos del sistema (activos e inactivos) con información básica del owner.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canRead_Groups`
- **Reglas de ownership:** No aplica. Cualquier usuario con el permiso puede ver todos los grupos.

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

**Sin parámetros de ruta, query ni body**

#### Responses

##### 200 - SGRRD200
**Cuándo ocurre:** Siempre que la consulta se ejecute exitosamente (incluso si no hay grupos)

```json
{
  "statusCode": 200,
  "intOpCode": "SGRRD200",
  "message": "Groups retrieved successfully.",
  "data": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "Equipo de Desarrollo",
      "description": "Grupo para desarrolladores del proyecto X",
      "owner": "Juan Alberto Pérez",
      "status": 1
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "Equipo de QA",
      "description": "Grupo de Quality Assurance",
      "owner": "María López García",
      "status": 2
    }
  ]
}
```

##### 403 - EGRFB403
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canRead_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

#### Lógica de Negocio
- Retorna TODOS los grupos sin filtrar por status
- El campo 'owner' se construye concatenando first_name, middle_name (si existe) y last_name del usuario
- No hay paginación implementada

#### Dependencias
- **Repositorios:** IGroupRepositorie.GetGroupsAsync
- **Servicios:** IAuthContextService.HasPermission
- **Helpers:** Dapper para ejecutar SQL raw
- **Entidades:** Group (tabla: group), User (tabla: user)
- **Claims JWT:** permission

#### Notas para Frontend
- Perfecto para listar grupos en una tabla o lista
- El campo 'status' es numérico (1 = Active, 2 = Inactive)
- Mostrar indicador visual diferente para grupos inactivos
- Si data está vacío, mostrar mensaje de 'No hay grupos creados'
- El campo 'owner' ya viene formateado como nombre completo
- NO hay paginación, considerar implementarla en frontend si hay muchos grupos

---

### 4. PATCH - Editar nombre y descripción de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}`  
**Endpoint Interno:** `PATCH /api/Group/EditGroup/{groupId}`

Permite al owner de un grupo editar su nombre y descripción. El grupo debe estar activo.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canUpdate_Groups`
- **Reglas de ownership:**
  - Solo el owner del grupo (CreatedByUserId) puede editarlo
  - Si requesterId != owner, retorna 403
  - SuperAdmin NO tiene privilegios especiales para este endpoint

#### Request

**Parámetros de ruta:**
- `groupId` (requerido, UUID): ID del grupo a editar
  - Ejemplo: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

**Body:**
```json
{
  "Name": "Equipo de Desarrollo Backend",
  "Description": "Grupo actualizado para desarrolladores backend"
}
```

**Campos requeridos:** Name, Description

**Validaciones:**
- El groupId debe existir en la tabla group
- El grupo debe tener Status.Active (no se puede editar un grupo inactivo)
- El requesterId (del JWT) debe ser el owner del grupo

#### Responses

##### 200 - SGRUP200
**Cuándo ocurre:** Cuando el grupo se actualiza exitosamente

```json
{
  "statusCode": 200,
  "intOpCode": "SGRUP200",
  "message": "Group updated successfully.",
  "data": []
}
```

##### 401 - EGRAU401
**Cuándo ocurre:** Cuando no se puede obtener el userId del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403 (Sin permiso)
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canUpdate_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 403 - EGRFB403 (No es owner)
**Cuándo ocurre:** Cuando el requesterId no es el owner del grupo

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can edit the group.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el groupId no existe en la base de datos

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 409 - EGRCF409
**Cuándo ocurre:** Cuando el grupo tiene Status.Inactive

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The group is already inactive.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while editing the group.",
  "data": []
}
```

#### Lógica de Negocio
- Solo el owner del grupo puede editarlo
- No se puede editar un grupo inactivo
- Se usa AutoMapper para mapear EditGroupRequest a la entidad Group existente
- El orden de validación es: existencia → ownership → status

#### Dependencias
- **Repositorios:** IGroupRepositorie.EditGroupAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Helpers:** AutoMapper
- **Entidades:** Group (tabla: group)
- **Claims JWT:** NameIdentifier, permission

#### Notas para Frontend
- Solo mostrar opción de editar si el usuario actual es el owner
- Deshabilitar edición si el grupo está inactivo
- Mostrar mensaje claro si se intenta editar un grupo inactivo (409)
- Después de editar exitosamente, refrescar vista del grupo
- El endpoint no valida unicidad de nombre al editar (inferido, no explícito en código)

---

### 5. GET - Obtener detalles completos de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}`  
**Endpoint Interno:** `GET /api/Group/GetGroupById/{groupId}`

Retorna información detallada de un grupo incluyendo sus miembros.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canRead_Groups`
- **Reglas de ownership:** No aplica. Cualquier usuario con el permiso puede ver cualquier grupo.

#### Request

**Parámetros de ruta:**
- `groupId` (requerido, UUID): ID del grupo a consultar
  - Ejemplo: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

#### Responses

##### 200 - SGRRD200
**Cuándo ocurre:** Cuando el grupo existe

```json
{
  "statusCode": 200,
  "intOpCode": "SGRRD200",
  "message": "Group retrieved successfully.",
  "data": [
    {
      "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "Name": "Equipo de Desarrollo",
      "Description": "Grupo para desarrolladores del proyecto X",
      "CreatedByUserId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "Owner": "Juan Alberto Pérez",
      "Status": 1,
      "Members": [
        {
          "Id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
          "UserName": "jperez",
          "CompleteName": "Juan Pérez García"
        },
        {
          "Id": "d4e5f6a7-b8c9-0123-def1-234567890123",
          "UserName": "mlopez",
          "CompleteName": "María López"
        }
      ]
    }
  ]
}
```

##### 403 - EGRFB403
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canRead_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el groupId no existe

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while retrieving the group.",
  "data": []
}
```

#### Lógica de Negocio
- Retorna el grupo con todos sus miembros en una sola consulta
- No filtra por status, puede retornar grupos inactivos
- Los miembros se obtienen de la tabla group_members con join a user
- El campo Owner se construye concatenando nombres del creator
- Si el grupo no tiene miembros, Members es un array vacío

#### Dependencias
- **Repositorios:** IGroupRepositorie.GetGroupByIdAsync
- **Servicios:** IAuthContextService.HasPermission
- **Helpers:** Dapper para ejecutar SQL raw
- **Entidades:** Group (tabla: group), User (tabla: user), GroupMembers (tabla: group_members)
- **Claims JWT:** permission

#### Notas para Frontend
- Perfecto para modal de detalle o vista individual del grupo
- El resultado viene en data[0] (array con un solo elemento)
- Members puede estar vacío si el grupo no tiene miembros
- CreatedByUserId es útil para determinar si el usuario actual es el owner
- Status numérico: 1 = Active, 2 = Inactive
- CompleteName de miembros ya viene formateado

---

### 6. GET - Obtener lista de miembros de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}/members`  
**Endpoint Interno:** `GET /api/Group/GetGroupMembers/{groupId}`

Retorna la lista de miembros de un grupo específico.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canRead_Groups`
- **Reglas de ownership:** No aplica. Cualquier usuario con el permiso puede ver miembros de cualquier grupo.

#### Request

**Parámetros de ruta:**
- `groupId` (requerido, UUID): ID del grupo
  - Ejemplo: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

#### Responses

##### 200 - SGRRD200
**Cuándo ocurre:** Cuando el grupo existe (incluso si no tiene miembros)

```json
{
  "statusCode": 200,
  "intOpCode": "SGRRD200",
  "message": "Group members retrieved successfully.",
  "data": [
    {
      "Id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "UserName": "jperez",
      "CompleteName": "Juan Pérez García"
    },
    {
      "Id": "d4e5f6a7-b8c9-0123-def1-234567890123",
      "UserName": "mlopez",
      "CompleteName": "María López"
    }
  ]
}
```

##### 403 - EGRFB403
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canRead_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el groupId no existe

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while retrieving the group members.",
  "data": []
}
```

#### Lógica de Negocio
- Primero valida que el grupo existe (con EF Core)
- Luego obtiene los miembros con SQL raw (Dapper)
- Si el grupo existe pero no tiene miembros, retorna array vacío con 200
- No filtra por status del grupo ni de los usuarios

#### Dependencias
- **Repositorios:** IGroupRepositorie.GetMembersAsync
- **Servicios:** IAuthContextService.HasPermission
- **Helpers:** Dapper para SQL raw, EF Core para validación de existencia
- **Entidades:** Group (tabla: group), User (tabla: user), GroupMembers (tabla: group_members)
- **Claims JWT:** permission

#### Notas para Frontend
- Perfecto para listar miembros en una tabla/lista
- Si data está vacío, el grupo existe pero no tiene miembros
- Id es el id del usuario, NO el id del registro group_members
- UserName y CompleteName son útiles para mostrar en UI
- Considerar refrescar después de AddMember o RemoveMember

---

### 7. DELETE - Eliminar un miembro de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}/members`  
**Endpoint Interno:** `DELETE /api/Group/RemoveMember`

Permite al owner de un grupo eliminar un miembro. El grupo debe estar activo.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canUpdate_Groups`
- **Reglas de ownership:**
  - Solo el owner del grupo puede eliminar miembros
  - Si requesterId != owner, retorna 403
  - SuperAdmin NO tiene privilegios especiales

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

**Body:**
```json
{
  "GroupId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "memberId": "b2c3d4e5-f6a7-8901-bcde-f12345678901"
}
```

**Campos requeridos:** GroupId, memberId

**Validaciones:**
- El grupo debe existir y estar activo (Status.Active)
- El requesterId (del JWT) debe ser el owner del grupo
- El memberId debe existir en la tabla group_members para ese grupo

#### Responses

##### 200 - SGRMB200
**Cuándo ocurre:** Cuando el miembro se elimina exitosamente

```json
{
  "statusCode": 200,
  "intOpCode": "SGRMB200",
  "message": "Group member removed successfully.",
  "data": []
}
```

##### 401 - EGRAU401
**Cuándo ocurre:** Cuando no se puede obtener el userId del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403 (Sin permiso)
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canUpdate_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 403 - EGRFB403 (No es owner)
**Cuándo ocurre:** Cuando el requesterId no es el owner del grupo

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can remove members from the group.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el memberId no está en la tabla group_members para ese grupo

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified member was not found in the group.",
  "data": []
}
```

##### 409 - EGRCF409
**Cuándo ocurre:** Cuando el grupo no existe o su Status != Active

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The specified group was not found or is inactive.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while removing the member from the group.",
  "data": []
}
```

#### Lógica de Negocio
- Solo el owner del grupo puede eliminar miembros
- El grupo debe estar activo
- Se usa ExecuteDeleteAsync de EF Core para eliminar directamente en DB
- Si affectedRows es 0, significa que el miembro no existía en el grupo

#### Dependencias
- **Repositorios:** IGroupRepositorie.RemoveMemberAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Entidades:** Group (tabla: group), GroupMembers (tabla: group_members)
- **Claims JWT:** NameIdentifier, permission

#### Notas para Frontend
- Solo mostrar opción de eliminar si el usuario actual es el owner
- Después de eliminar exitosamente, refrescar lista de miembros
- Mostrar confirmación antes de eliminar
- Deshabilitar si el grupo está inactivo
- El error 404 específicamente indica que el miembro no está en el grupo (no que el usuario no existe)

---

### 8. PATCH - Desactivar un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}/deactivate`  
**Endpoint Interno:** `PATCH /api/Group/{groupId}/deactivate`

Cambia el estado de un grupo a Inactive. Solo el owner o el SuperAdmin pueden desactivar un grupo.

#### Autenticación y Permisos
- **Requiere autenticación:** ✓ (Bearer JWT)
- **Permisos requeridos:** `canDelete_Groups`
- **Reglas de ownership:**
  - El owner del grupo puede desactivarlo
  - **EXCEPCIÓN:** El SuperAdmin (`00000000-0000-0000-0000-000000000001`) TAMBIÉN puede desactivar cualquier grupo
  - Si requesterId no es owner NI SuperAdmin, retorna 403

#### Request

**Parámetros de ruta:**
- `groupId` (requerido, UUID): ID del grupo a desactivar
  - Ejemplo: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Headers:**
- `Authorization` (requerido): Bearer token con JWT válido

**Sin body**

**Validaciones:**
- El grupo debe existir
- El grupo NO debe estar ya inactivo (Status.Inactive)
- El requesterId debe ser owner O SuperAdmin

#### Responses

##### 200 - SGRDL200
**Cuándo ocurre:** Cuando el grupo se desactiva exitosamente

```json
{
  "statusCode": 200,
  "intOpCode": "SGRDL200",
  "message": "Group deactivated successfully.",
  "data": []
}
```

##### 401 - EGRAU401
**Cuándo ocurre:** Cuando no se puede obtener el userId del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403 (Sin permiso)
**Cuándo ocurre:** Cuando el usuario no tiene el permiso 'canDelete_Groups'

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 403 - EGRFB403 (No es owner ni SuperAdmin)
**Cuándo ocurre:** Cuando el requesterId no es owner ni SuperAdmin

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can deactivate the group.",
  "data": []
}
```

##### 404 - EGRNF404
**Cuándo ocurre:** Cuando el groupId no existe

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 409 - EGRCF409
**Cuándo ocurre:** Cuando el grupo ya tiene Status.Inactive

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The group is already inactive.",
  "data": []
}
```

##### 500 - EGRIN500
**Cuándo ocurre:** Error inesperado no catalogado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while deactivating the group.",
  "data": []
}
```

#### Lógica de Negocio
- La desactivación es un soft delete (cambia Status a Inactive)
- El owner SIEMPRE puede desactivar su grupo
- El SuperAdmin (`00000000-0000-0000-0000-000000000001`) puede desactivar CUALQUIER grupo
- No se puede desactivar un grupo ya inactivo (validación de idempotencia)
- Único endpoint donde SuperAdmin tiene privilegios especiales

#### Dependencias
- **Repositorios:** IGroupRepositorie.DeactivateGroupAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Entidades:** Group (tabla: group)
- **Claims JWT:** NameIdentifier, permission

#### Notas para Frontend
- Mostrar opción de desactivar solo si el usuario es owner O SuperAdmin
- Considerar mostrar confirmación antes de desactivar (es una acción importante)
- Después de desactivar, actualizar vista (el grupo no debería ser editable)
- Deshabilitar botón si el grupo ya está inactivo
- NO es eliminación física, el grupo sigue existiendo en la DB

---

## Inconsistencias Detectadas

### 1. Gateway endpoint conflict (Severidad: Baja)
**Endpoint:** AddGroup / GetGroups

**Problema:** El comentario indica que ambos endpoints corresponden a `https://mi-gateway.onrender.com/groups/`, uno con POST y otro con GET. Esto es correcto si el API Gateway enruta por método HTTP.

**Resolución:** Confirmar que el API Gateway enruta correctamente POST /groups/ a AddGroup y GET /groups/ a GetGroups.

---

### 2. Gateway endpoint suggests route param but body is used (Severidad: Media)
**Endpoint:** RemoveMember

**Problema:** El comentario indica `https://mi-gateway.onrender.com/groups/{groupId}/members` sugiriendo que groupId podría ser route parameter, pero el endpoint interno usa DELETE sin route params, enviando GroupId y memberId en el body. Esto es inusual para REST APIs donde DELETE típicamente usa route params.

**Resolución:** Considerar cambiar a `DELETE /api/Group/{groupId}/members/{memberId}` o aclarar en documentación del gateway.

---

### 3. No validation for unique name on edit (Severidad: Media)
**Endpoint:** EditGroup

**Problema:** El endpoint AddGroup valida que el nombre sea único, pero EditGroup no tiene validación explícita de unicidad al cambiar el nombre. Esto podría permitir duplicados.

**Resolución:** Agregar validación de unicidad en EditGroupAsync o documentar que es intencional.

---

### 4. Functional duplication (Severidad: Baja)
**Endpoints:** GetGroupById vs GetGroupMembers

**Problema:** GetGroupById retorna el grupo completo incluyendo miembros, mientras que GetGroupMembers retorna solo los miembros. Hay duplicación funcional.

**Resolución:** Documentado como diferente nivel de detalle. GetGroupMembers es más liviano.

---

### 5. Error message inaccuracy (Severidad: Baja)
**Endpoint:** DeactivateGroup

**Problema:** El mensaje 'Only the group owner can deactivate the group.' es técnicamente inexacto porque el SuperAdmin también puede desactivar cualquier grupo.

**Resolución:** Cambiar mensaje a 'Only the group owner or SuperAdmin can deactivate the group.'

---

### 6. Inconsistent intOpCode usage (Severidad: Info)
**Endpoints:** Todos

**Problema:** El intOpCode parece usar el patrón `{S/E}{GR}{operación}{statusCode}` donde S=Success, E=Error, GR=Groups. Sin embargo, la consistencia no está documentada formalmente.

**Resolución:** Documentar el patrón de intOpCode para que frontend pueda usarlo consistentemente.

---

### 7. Inconsistent intOpCode for different operations (Severidad: Baja)
**Endpoints:** AddMember, RemoveMember

**Problema:** AddMember y RemoveMember usan el mismo intOpCode 'SGRMB200' para éxito, cuando representan operaciones opuestas.

**Resolución:** Considerar usar códigos diferentes como SGRMBA200 (add) y SGRMBR200 (remove).

---

## Información Técnica Adicional

### Formato de Response Estándar
Todos los endpoints usan el mismo formato de respuesta:

```json
{
  "statusCode": 200,
  "intOpCode": "CODIGO_OPERACION",
  "message": "Mensaje descriptivo",
  "data": []
}
```

- `statusCode`: Código HTTP estándar
- `intOpCode`: Código interno de operación para identificación específica
- `message`: Mensaje descriptivo del resultado
- `data`: SIEMPRE es un array, incluso si está vacío

### Autenticación
- Se soporta Bearer token en header Authorization
- Alternativamente se puede enviar cookie 'jwt'
- El token debe ser un JWT válido firmado con la clave del servicio
- Claims requeridos:
  - `NameIdentifier`: Para identificar al usuario
  - `permission`: Para validar permisos (puede haber múltiples claims de este tipo)

### Base de Datos
- PostgreSQL con naming convention snake_case
- Tablas principales:
  - `group`: Grupos del sistema
  - `user`: Usuarios del sistema
  - `group_members`: Relación muchos a muchos entre grupos y usuarios

### Tecnologías
- ASP.NET Core
- Entity Framework Core (con snake_case naming convention)
- Dapper (para consultas SQL raw optimizadas)
- AutoMapper (para mapeo de DTOs)
- JWT Bearer Authentication
