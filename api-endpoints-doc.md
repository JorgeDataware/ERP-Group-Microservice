# Groups Microservice API - Documentación de Endpoints

## Resumen del Servicio

**Nombre:** Groups Microservice

**Descripción:** Microservicio para la gestión de grupos de trabajo. Permite crear grupos, administrar miembros, editar información de grupos y desactivar grupos. Implementa control de acceso basado en permisos JWT y reglas de propiedad (owner).

**Tecnología:** .NET Core 8 + Entity Framework Core + Dapper + PostgreSQL

**Autenticación:** JWT Bearer Token (header Authorization o cookie 'jwt')

**Base Internal Routes:**
- `api/Group`

### Notas Importantes

- Todos los endpoints devuelven un `StandardResponse` con estructura: `{ statusCode, intOpCode, message, data[] }`
- El campo `data` siempre es un array, incluso si está vacío
- La autenticación se puede enviar por header `Authorization` (Bearer token) o cookie `jwt`
- Los permisos se validan desde los claims del JWT con tipo `permission`
- El `userId` se extrae del claim `NameIdentifier` del JWT
- Las reglas de ownership se validan contra el campo `CreatedByUserId` del grupo
- SuperAdmin (`00000000-0000-0000-0000-000000000001`) tiene permisos especiales en algunos endpoints

---

## Enums Globales

### Status

Estado de entidades (Group, User)

| Nombre | Valor | Descripción |
|--------|-------|-------------|
| Active | 1 | Entidad activa y operativa |
| Inactive | 2 | Entidad desactivada |

---

## Permisos Globales

| Nombre | Descripción | Constante |
|--------|-------------|-----------|
| canCreate_Groups | Permite crear nuevos grupos | GroupPermissions.CanCreate |
| canRead_Groups | Permite leer grupos y sus miembros | GroupPermissions.CanRead |
| canUpdate_Groups | Permite editar grupos, agregar y remover miembros | GroupPermissions.CanUpdate |
| canDelete_Groups | Permite desactivar grupos | GroupPermissions.CanDelete |

---

## Constantes Globales

| Nombre | Valor | Descripción |
|--------|-------|-------------|
| SuperAdminId | 00000000-0000-0000-0000-000000000001 | UUID del super administrador del sistema. Tiene permisos especiales para desactivar grupos sin ser owner |

---

## Tabla Resumen de Endpoints

| Method | Gateway Endpoint | Internal Endpoint | Auth | Permissions | Summary |
|--------|------------------|-------------------|------|-------------|---------|
| POST | https://mi-gateway.onrender.com/groups/ | api/Group/AddGroup | Bearer | canCreate_Groups | Crear un nuevo grupo |
| POST | https://mi-gateway.onrender.com/groups/members | api/Group/AddMember | Bearer | canUpdate_Groups | Agregar un miembro a un grupo |
| GET | https://mi-gateway.onrender.com/groups/ | api/Group/GetGroups | Bearer | canRead_Groups | Obtener todos los grupos |
| PATCH | https://mi-gateway.onrender.com/groups/{groupId} | api/Group/EditGroup/{groupId} | Bearer | canUpdate_Groups | Editar información de un grupo |
| GET | https://mi-gateway.onrender.com/groups/{groupId} | api/Group/GetGroupById/{groupId} | Bearer | canRead_Groups | Obtener detalle completo de un grupo |
| GET | https://mi-gateway.onrender.com/groups/{groupId}/members | api/Group/GetGroupMembers/{groupId} | Bearer | canRead_Groups | Obtener miembros de un grupo |
| DELETE | https://mi-gateway.onrender.com/groups/{groupId}/members | api/Group/RemoveMember | Bearer | canUpdate_Groups | Remover un miembro de un grupo |
| PATCH | https://mi-gateway.onrender.com/groups/{groupId}/deactivate | api/Group/{groupId}/deactivate | Bearer | canDelete_Groups | Desactivar un grupo |

---

## Endpoints Detallados

### 1. POST - Crear un nuevo grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/`

**Internal Endpoint:** `api/Group/AddGroup`

**Archivo fuente:** `Controllers/GroupController.cs:75`

#### Descripción

Crea un nuevo grupo de trabajo. El usuario autenticado se convierte automáticamente en el propietario (owner) del grupo. Valida que no exista un grupo con el mismo nombre y que el usuario exista en la base de datos.

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canCreate_Groups`
- **Reglas de ownership:** El usuario autenticado se establece como CreatedByUserId (owner) del grupo

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token JWT
  - Ejemplo: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
- `Content-Type` (requerido): `application/json`

**Cookies opcionales:**
- `jwt`: Token JWT alternativo si no se envía el header Authorization

**Body Schema:**

```json
{
  "name": "string (requerido)",
  "description": "string (requerido)"
}
```

**Body Example:**

```json
{
  "name": "Equipo de Desarrollo",
  "description": "Grupo para el equipo de desarrollo de software"
}
```

#### Validaciones

- `name` debe ser único (no puede existir otro grupo con ese nombre)
- El usuario debe existir en la tabla 'user'

#### Responses

##### 201 - SGRCR201

**Cuándo ocurre:** El grupo se creó exitosamente

**Respuesta:**

```json
{
  "statusCode": 201,
  "intOpCode": "SGRCR201",
  "message": "Group created successfully.",
  "data": []
}
```

##### 401 - EGRAU401

**Cuándo ocurre:** El userId no pudo extraerse del JWT (claim NameIdentifier inválido o faltante)

**Respuesta:**

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "User authentication is required to add a group.",
  "data": []
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canCreate_Groups' en sus claims JWT

**Respuesta:**

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El userId extraído del JWT no existe en la tabla 'user'

**Respuesta:**

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified user was not found.",
  "data": []
}
```

##### 409 - EGRCF409

**Cuándo ocurre:** Ya existe un grupo con el mismo 'name'

**Respuesta:**

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "A group with the specified name already exists.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado (catch-all en switch default)

**Respuesta:**

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while adding the group.",
  "data": []
}
```

#### Lógica de Negocio

- El nombre del grupo debe ser único en toda la base de datos
- El usuario que crea el grupo se convierte automáticamente en owner (CreatedByUserId)
- El grupo se crea con Status = Active por defecto
- El usuario debe existir en la tabla 'user' antes de poder crear un grupo

#### Dependencias

- **Repositorios:** IGroupRepositorie.AddGroupAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Helpers:** AutoMapper
- **Entidades:** Group, User
- **Claims:** NameIdentifier (userId), permission: canCreate_Groups

#### Notas para Frontend

- Este endpoint NO devuelve el ID del grupo creado en la respuesta actual, solo confirma creación
- Si necesitas el groupId después de crear, debes llamar a GetGroups o buscar por nombre
- Ideal para formularios de creación de grupos
- Muestra mensaje de error específico al usuario si el nombre ya existe (409)
- El backend valida permisos, pero el frontend debería ocultar/deshabilitar UI si el usuario no tiene canCreate_Groups

---

### 2. POST - Agregar un miembro a un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/members`

**Internal Endpoint:** `api/Group/AddMember`

**Archivo fuente:** `Controllers/GroupController.cs:104`

#### Descripción

Agrega un usuario como miembro de un grupo existente. Solo el propietario (owner/creador) del grupo puede agregar miembros. El grupo debe estar activo (Status = Active).

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canUpdate_Groups`
- **Reglas de ownership:** Solo el owner del grupo (CreatedByUserId) puede agregar miembros. El requesterId debe coincidir con group.CreatedByUserId

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token JWT
- `Content-Type` (requerido): `application/json`

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body Schema:**

```json
{
  "groupId": "uuid (requerido)",
  "userId": "uuid (requerido)"
}
```

**Body Example:**

```json
{
  "groupId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "660e8400-e29b-41d4-a716-446655440001"
}
```

#### Validaciones

- El grupo debe existir y estar activo (Status = Active)
- El usuario a agregar debe existir en la tabla 'user'
- El usuario no debe ser miembro del grupo ya
- El requesterId debe ser el owner del grupo

#### Responses

##### 200 - SGRMB200

**Cuándo ocurre:** El miembro se agregó exitosamente al grupo

```json
{
  "statusCode": 200,
  "intOpCode": "SGRMB200",
  "message": "Group member added successfully.",
  "data": []
}
```

##### 401 - EGRAU401

**Cuándo ocurre:** El userId no pudo extraerse del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canUpdate_Groups' O no es el owner del grupo

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can add members to the group.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El userId a agregar no existe en la tabla 'user'

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified user was not found.",
  "data": []
}
```

##### 409 - EGRCF409

**Cuándo ocurre:** El grupo no existe o está inactivo, O el miembro ya existe en el grupo

**Nota:** El mensaje puede variar: "The specified group was not found or is inactive." o "The specified member is already part of the group."

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The specified member is already part of the group.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado

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
- El grupo debe estar en estado Active
- El usuario a agregar debe existir en la base de datos
- No se puede agregar un usuario que ya es miembro del grupo
- Se crea un nuevo registro en la tabla 'group_members' con un nuevo Guid

#### Dependencias

- **Repositorios:** IGroupRepositorie.AddMemberAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Entidades:** Group, User, GroupMembers
- **Claims:** NameIdentifier (userId), permission: canUpdate_Groups

#### Notas para Frontend

- Este endpoint requiere tanto permiso general como ownership
- Ideal para componentes de gestión de miembros de grupo
- Debe permitir al frontend seleccionar usuarios de una lista para agregarlos
- Mostrar error claro si el usuario ya es miembro (409)
- Mostrar error si el grupo está inactivo (409)
- Después de agregar, conviene refrescar la lista de miembros del grupo
- Solo habilitar UI si el usuario actual es el owner del grupo

---

### 3. GET - Obtener todos los grupos

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/`

**Internal Endpoint:** `api/Group/GetGroups`

**Archivo fuente:** `Controllers/GroupController.cs:130`

#### Descripción

Obtiene una lista completa de todos los grupos registrados en el sistema, incluyendo información básica y el nombre del propietario. No hay paginación implementada.

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canRead_Groups`
- **Reglas de ownership:** No aplica - listado general para usuarios con permiso de lectura

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token JWT

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body:** No requiere body

#### Responses

##### 200 - SGRRD200

**Cuándo ocurre:** La consulta se ejecutó exitosamente (puede devolver array vacío si no hay grupos)

**Nota:** `owner` es el nombre completo concatenado del usuario (FirstName + MiddleName + LastName). `status` es numérico: 1 = Active, 2 = Inactive

```json
{
  "statusCode": 200,
  "intOpCode": "SGRRD200",
  "message": "Groups retrieved successfully.",
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Equipo de Desarrollo",
      "description": "Grupo para el equipo de desarrollo de software",
      "owner": "Juan Carlos Pérez",
      "status": 1
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "name": "Equipo de QA",
      "description": "Grupo de aseguramiento de calidad",
      "owner": "María González López",
      "status": 1
    }
  ]
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canRead_Groups' en sus claims JWT

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

#### Lógica de Negocio

- Devuelve TODOS los grupos sin filtrar por status (incluye Active e Inactive)
- No hay paginación implementada
- El owner se calcula con CONCAT_WS concatenando FirstName, MiddleName (si existe) y LastName
- Usa Dapper para consulta SQL directa (no Entity Framework)

#### Dependencias

- **Repositorios:** IGroupRepositorie.GetGroupsAsync
- **Servicios:** IAuthContextService.HasPermission
- **Helpers:** Dapper
- **Entidades:** Group, User
- **Claims:** permission: canRead_Groups

#### Notas para Frontend

- Ideal para poblar tablas/listas de grupos
- No hay paginación, considerar agregar paginación en frontend si la lista crece mucho
- El campo `status` es numérico (1 = Active, 2 = Inactive), convertir a texto legible en UI
- El campo `owner` ya viene como texto listo para mostrar
- Guardar el `id` de cada grupo para operaciones posteriores (editar, ver detalle, etc.)
- Puede devolver array vacío si no hay grupos, manejar UI para lista vacía
- NO devuelve información de miembros, solo datos básicos del grupo

---

### 4. PATCH - Editar información de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}`

**Internal Endpoint:** `api/Group/EditGroup/{groupId}`

**Archivo fuente:** `Controllers/GroupController.cs:146`

#### Descripción

Actualiza el nombre y/o descripción de un grupo existente. Solo el propietario (owner/creador) del grupo puede editarlo. El grupo debe estar activo (Status = Active).

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canUpdate_Groups`
- **Reglas de ownership:** Solo el owner del grupo (CreatedByUserId) puede editarlo. El userId del JWT debe coincidir con group.CreatedByUserId

#### Request

**Route Params:**
- `groupId` (uuid, requerido): ID del grupo a editar

**Headers:**
- `Authorization` (requerido): Bearer token JWT
- `Content-Type` (requerido): `application/json`

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body Schema:**

```json
{
  "name": "string (requerido)",
  "description": "string (requerido)"
}
```

**Body Example:**

```json
{
  "name": "Equipo de Desarrollo Frontend",
  "description": "Grupo especializado en desarrollo de interfaces de usuario"
}
```

#### Validaciones

- El grupo debe existir
- El grupo debe estar activo (Status = Active)
- El userId debe ser el owner del grupo (CreatedByUserId)

#### Responses

##### 200 - SGRUP200

**Cuándo ocurre:** El grupo se actualizó exitosamente

```json
{
  "statusCode": 200,
  "intOpCode": "SGRUP200",
  "message": "Group updated successfully.",
  "data": []
}
```

##### 401 - EGRAU401

**Cuándo ocurre:** El userId no pudo extraerse del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canUpdate_Groups' O no es el owner del grupo

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can edit the group.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El groupId no existe en la base de datos

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 409 - EGRCF409

**Cuándo ocurre:** El grupo tiene Status = Inactive

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The group is already inactive.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado

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
- Se actualizan ambos campos (name y description) mediante AutoMapper
- ⚠️ **No se valida unicidad de nombre** (puede generar conflictos si se intenta usar nombre duplicado)

#### Dependencias

- **Repositorios:** IGroupRepositorie.EditGroupAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Helpers:** AutoMapper
- **Entidades:** Group
- **Claims:** NameIdentifier (userId), permission: canUpdate_Groups

#### Notas para Frontend

- Usar en formularios de edición de grupos
- Validar en frontend que el usuario actual es el owner antes de mostrar UI de edición
- El groupId debe venir de la URL o contexto previo (ej: desde tabla de grupos)
- Después de editar, refrescar vista de detalle del grupo o lista de grupos
- Manejar 409 mostrando mensaje que no se puede editar grupo inactivo
- ⚠️ Considerar que NO se valida unicidad de nombre en el backend (puede causar problemas)

---

### 5. GET - Obtener detalle completo de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}`

**Internal Endpoint:** `api/Group/GetGroupById/{groupId}`

**Archivo fuente:** `Controllers/GroupController.cs:174`

#### Descripción

Obtiene información detallada de un grupo específico, incluyendo datos básicos del grupo, información del propietario, y lista completa de miembros con sus datos personales.

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canRead_Groups`
- **Reglas de ownership:** No aplica - cualquier usuario con permiso de lectura puede ver detalles de cualquier grupo

#### Request

**Route Params:**
- `groupId` (uuid, requerido): ID del grupo a consultar

**Headers:**
- `Authorization` (requerido): Bearer token JWT

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body:** No requiere body

#### Validaciones

- El grupo debe existir

#### Responses

##### 200 - SGRRD200

**Cuándo ocurre:** El grupo existe y se recuperó exitosamente con sus miembros

**Nota:** `data` es un array con un solo elemento. `status` es numérico (1=Active, 2=Inactive). `members` puede ser array vacío si no hay miembros.

```json
{
  "statusCode": 200,
  "intOpCode": "SGRRD200",
  "message": "Group retrieved successfully.",
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Equipo de Desarrollo",
      "description": "Grupo para el equipo de desarrollo de software",
      "createdByUserId": "770e8400-e29b-41d4-a716-446655440002",
      "owner": "Juan Carlos Pérez",
      "status": 1,
      "members": [
        {
          "id": "880e8400-e29b-41d4-a716-446655440003",
          "userName": "jperez",
          "completeName": "Juan Carlos Pérez"
        },
        {
          "id": "990e8400-e29b-41d4-a716-446655440004",
          "userName": "mgonzalez",
          "completeName": "María González López"
        }
      ]
    }
  ]
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canRead_Groups' en sus claims JWT

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El groupId no existe en la base de datos

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while retrieving the group.",
  "data": []
}
```

#### Lógica de Negocio

- Devuelve información completa del grupo incluyendo lista de miembros
- No filtra por status del grupo (devuelve tanto Active como Inactive)
- Usa Dapper con dos queries SQL separadas (una para grupo, otra para miembros)
- El owner se calcula concatenando FirstName, MiddleName y LastName
- completeName de miembros también se calcula con CONCAT_WS

#### Dependencias

- **Repositorios:** IGroupRepositorie.GetGroupByIdAsync
- **Servicios:** IAuthContextService.HasPermission
- **Helpers:** Dapper
- **Entidades:** Group, User, GroupMembers
- **Claims:** permission: canRead_Groups

#### Notas para Frontend

- Ideal para modales de detalle o páginas de vista de grupo
- Devuelve toda la información necesaria en una sola llamada (grupo + miembros)
- El array `data` tiene un solo elemento, acceder como `data[0]`
- `members` puede ser array vacío, manejar UI apropiadamente
- `createdByUserId` es útil para determinar si el usuario actual es owner y mostrar/ocultar botones de edición/eliminación
- `status` numérico (1=Active, 2=Inactive), convertir a texto en UI
- `userName` y `completeName` de miembros listos para mostrar

---

### 6. GET - Obtener miembros de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}/members`

**Internal Endpoint:** `api/Group/GetGroupMembers/{groupId}`

**Archivo fuente:** `Controllers/GroupController.cs:194`

#### Descripción

Obtiene la lista completa de miembros de un grupo específico, incluyendo ID, username y nombre completo de cada miembro.

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canRead_Groups`
- **Reglas de ownership:** No aplica - cualquier usuario con permiso de lectura puede ver miembros de cualquier grupo

#### Request

**Route Params:**
- `groupId` (uuid, requerido): ID del grupo del cual obtener los miembros

**Headers:**
- `Authorization` (requerido): Bearer token JWT

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body:** No requiere body

#### Validaciones

- El grupo debe existir

#### Responses

##### 200 - SGRRD200

**Cuándo ocurre:** La consulta se ejecutó exitosamente (puede devolver array vacío si no hay miembros)

```json
{
  "statusCode": 200,
  "intOpCode": "SGRRD200",
  "message": "Group members retrieved successfully.",
  "data": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440003",
      "userName": "jperez",
      "completeName": "Juan Carlos Pérez"
    },
    {
      "id": "990e8400-e29b-41d4-a716-446655440004",
      "userName": "mgonzalez",
      "completeName": "María González López"
    }
  ]
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canRead_Groups' en sus claims JWT

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "You do not have permission to perform this action.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El groupId no existe en la base de datos

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while retrieving the group members.",
  "data": []
}
```

#### Lógica de Negocio

- Devuelve solo usuarios que están en la tabla group_members para el groupId especificado
- NO valida si el grupo está activo o inactivo, solo si existe
- Usa Dapper con query SQL directa
- completeName se calcula con CONCAT_WS(FirstName, MiddleName, LastName)

#### Dependencias

- **Repositorios:** IGroupRepositorie.GetMembersAsync
- **Servicios:** IAuthContextService.HasPermission
- **Helpers:** Dapper
- **Entidades:** Group, User, GroupMembers
- **Claims:** permission: canRead_Groups

#### Notas para Frontend

- Útil para componentes que solo necesitan lista de miembros sin información completa del grupo
- Más ligero que GetGroupById si solo necesitas miembros
- Puede devolver array vacío si el grupo no tiene miembros, manejar UI apropiadamente
- `id` es el userId del miembro (útil para operaciones posteriores como remover)
- `userName` y `completeName` listos para mostrar en listas/tablas
- Después de agregar/remover miembros, llamar este endpoint para refrescar lista

---

### 7. DELETE - Remover un miembro de un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}/members`

**Internal Endpoint:** `api/Group/RemoveMember`

**Archivo fuente:** `Controllers/GroupController.cs:218`

#### Descripción

Elimina un usuario de la lista de miembros de un grupo. Solo el propietario (owner/creador) del grupo puede remover miembros. El grupo debe estar activo (Status = Active).

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canUpdate_Groups`
- **Reglas de ownership:** Solo el owner del grupo (CreatedByUserId) puede remover miembros. El requesterId debe coincidir con group.CreatedByUserId

#### Request

**Headers:**
- `Authorization` (requerido): Bearer token JWT
- `Content-Type` (requerido): `application/json`

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body Schema:**

```json
{
  "groupId": "uuid (requerido)",
  "userId": "uuid (requerido)"
}
```

**Body Example:**

```json
{
  "groupId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "660e8400-e29b-41d4-a716-446655440001"
}
```

#### Validaciones

- El grupo debe existir y estar activo (Status = Active)
- El miembro debe existir en la tabla group_members para ese grupo
- El requesterId debe ser el owner del grupo

#### Responses

##### 200 - SGRMB200

**Cuándo ocurre:** El miembro se removió exitosamente del grupo

```json
{
  "statusCode": 200,
  "intOpCode": "SGRMB200",
  "message": "Group member removed successfully.",
  "data": []
}
```

##### 401 - EGRAU401

**Cuándo ocurre:** El userId no pudo extraerse del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canUpdate_Groups' O no es el owner del grupo

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can remove members from the group.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El userId no existe en la tabla group_members para ese groupId (no es miembro del grupo)

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified member was not found in the group.",
  "data": []
}
```

##### 409 - EGRCF409

**Cuándo ocurre:** El grupo no existe o está inactivo (Status = Inactive)

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The specified group was not found or is inactive.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while removing the member from the group.",
  "data": []
}
```

#### Lógica de Negocio

- Solo el owner del grupo puede remover miembros
- El grupo debe estar en estado Active
- Usa ExecuteDeleteAsync de Entity Framework (eliminación física, no lógica)
- Si affectedRows es 0, significa que el miembro no existía en el grupo

#### Dependencias

- **Repositorios:** IGroupRepositorie.RemoveMemberAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Entidades:** Group, GroupMembers
- **Claims:** NameIdentifier (userId), permission: canUpdate_Groups

#### Notas para Frontend

- Ideal para botones de 'Remover' o 'Eliminar miembro' en listas de miembros
- Solo habilitar UI si el usuario actual es el owner del grupo
- Después de remover, refrescar lista de miembros
- Considerar confirmación antes de remover (modal de confirmación)
- Manejar error 404 si el miembro ya no está en el grupo (puede ocurrir con concurrencia)
- Manejar error 409 si el grupo está inactivo

---

### 8. PATCH - Desactivar un grupo

**Gateway Endpoint:** `https://mi-gateway.onrender.com/groups/{groupId}/deactivate`

**Internal Endpoint:** `api/Group/{groupId}/deactivate`

**Archivo fuente:** `Controllers/GroupController.cs:242`

#### Descripción

Cambia el estado de un grupo de Active a Inactive. Solo el propietario (owner/creador) del grupo o el SuperAdmin pueden desactivarlo. El grupo debe estar activo para poder desactivarse.

#### Autenticación y Autorización

- **Requiere autenticación:** Sí
- **Tipo:** Bearer Token JWT
- **Permisos requeridos:** `canDelete_Groups`
- **Reglas de ownership:** Solo el owner del grupo (CreatedByUserId) O el SuperAdmin (00000000-0000-0000-0000-000000000001) pueden desactivarlo. El userId del JWT debe coincidir con group.CreatedByUserId O ser SuperAdminId

#### Request

**Route Params:**
- `groupId` (uuid, requerido): ID del grupo a desactivar

**Headers:**
- `Authorization` (requerido): Bearer token JWT

**Cookies opcionales:**
- `jwt`: Token JWT alternativo

**Body:** No requiere body

#### Validaciones

- El grupo debe existir
- El grupo debe estar activo (Status = Active)
- El userId debe ser el owner del grupo O ser el SuperAdmin

#### Responses

##### 200 - SGRDL200

**Cuándo ocurre:** El grupo se desactivó exitosamente

```json
{
  "statusCode": 200,
  "intOpCode": "SGRDL200",
  "message": "Group deactivated successfully.",
  "data": []
}
```

##### 401 - EGRAU401

**Cuándo ocurre:** El userId no pudo extraerse del JWT

```json
{
  "statusCode": 401,
  "intOpCode": "EGRAU401",
  "message": "Invalid or missing user authentication.",
  "data": []
}
```

##### 403 - EGRFB403

**Cuándo ocurre:** El usuario no tiene el permiso 'canDelete_Groups' O no es el owner del grupo ni el SuperAdmin

```json
{
  "statusCode": 403,
  "intOpCode": "EGRFB403",
  "message": "Only the group owner can deactivate the group.",
  "data": []
}
```

##### 404 - EGRNF404

**Cuándo ocurre:** El groupId no existe en la base de datos

```json
{
  "statusCode": 404,
  "intOpCode": "EGRNF404",
  "message": "The specified group was not found.",
  "data": []
}
```

##### 409 - EGRCF409

**Cuándo ocurre:** El grupo ya tiene Status = Inactive

```json
{
  "statusCode": 409,
  "intOpCode": "EGRCF409",
  "message": "The group is already inactive.",
  "data": []
}
```

##### 500 - EGRIN500

**Cuándo ocurre:** Cualquier otro error no contemplado

```json
{
  "statusCode": 500,
  "intOpCode": "EGRIN500",
  "message": "An unexpected error occurred while deactivating the group.",
  "data": []
}
```

#### Lógica de Negocio

- Solo el owner del grupo O el SuperAdmin pueden desactivarlo
- SuperAdmin tiene bypass de ownership (no necesita ser owner)
- No se puede desactivar un grupo ya inactivo
- Es una desactivación lógica (Status = Inactive), no física
- Los miembros del grupo NO se eliminan al desactivar

#### Dependencias

- **Repositorios:** IGroupRepositorie.DeactivateGroupAsync
- **Servicios:** IAuthContextService.HasPermission, IAuthContextService.GetUserId
- **Entidades:** Group
- **Claims:** NameIdentifier (userId), permission: canDelete_Groups

#### Notas para Frontend

- Usar en botones de 'Desactivar' o 'Archivar' grupo
- Solo habilitar UI si el usuario actual es el owner O es SuperAdmin
- Considerar confirmación antes de desactivar (modal de confirmación)
- Después de desactivar, redirigir a lista de grupos o actualizar vista
- Manejar error 409 si el grupo ya está inactivo (puede ocurrir con concurrencia)
- El SuperAdmin puede desactivar cualquier grupo (validar en frontend si el usuario es SuperAdmin)
- Los miembros del grupo NO se eliminan, solo cambia el status

---

## Inconsistencias Detectadas

### ⚠️ Severidad: MEDIA

**Endpoint:** add-group

**Problema:** El repositorio devuelve `Result<Guid>` con el groupId creado, pero el controller no incluye este ID en la respuesta 201. El frontend no puede conocer el ID del grupo recién creado sin hacer otra consulta.

**Recomendación:** Considerar incluir el groupId en el campo 'data' de la respuesta 201.

---

### ⚠️ Severidad: ALTA

**Endpoint:** edit-group

**Problema:** No se valida unicidad del nombre al editar un grupo (solo se valida al crear). Esto podría permitir nombres duplicados.

**Recomendación:** Agregar validación de unicidad de nombre en EditGroupAsync similar a AddGroupAsync.

---

### ℹ️ Severidad: BAJA

**Endpoint:** get-group-by-id

**Problema:** Inconsistencia en naming conventions: `GetCompleteGroupDto` usa PascalCase (Id, Name, Description) mientras `GroupDto` usa camelCase (id, name, description).

**Recomendación:** Estandarizar las propiedades de los DTOs a una sola convención (preferiblemente camelCase para JSON).

---

### ⚠️ Severidad: MEDIA

**Endpoint:** remove-member

**Problema:** El endpoint usa método HTTP DELETE pero espera body con JSON. Algunos proxies/gateways HTTP pueden rechazar DELETE con body. RESTful típico usaría DELETE a `/groups/{groupId}/members/{userId}` sin body.

**Recomendación:** Considerar cambiar a route params: `DELETE /groups/{groupId}/members/{userId}`

---

### ℹ️ Severidad: BAJA

**Endpoints:** múltiples (GetGroups, GetGroupById, GetGroupMembers)

**Problema:** El mismo intOpCode 'SGRRD200' se usa para diferentes operaciones (GetGroups, GetGroupById, GetGroupMembers). Dificulta el tracking específico de operaciones en frontend.

**Recomendación:** Usar intOpCodes únicos por endpoint (ej: SGRGT200 para GetGroups, SGRGD200 para GetGroupById, SGRGM200 para GetGroupMembers).

---

### ℹ️ Severidad: BAJA

**Endpoints:** add-member, edit-group

**Problema:** Los comentarios 'Corresponde a:' para AddMember y EditGroup apuntan a la misma URL del gateway pero con métodos HTTP diferentes. El comentario de AddMember debería especificar el método POST.

**Recomendación:** Clarificar en comentarios cuando múltiples endpoints internos mapean a la misma ruta del gateway con diferentes métodos HTTP.

---

### ℹ️ Severidad: INFO

**Endpoints:** múltiples

**Problema:** El campo 'data' siempre es un array, incluso cuando solo se devuelve un elemento (GetGroupById). Esto puede ser confuso para consumers de la API.

**Recomendación:** Considerar usar 'data' como objeto o array según el contexto, o documentar claramente esta convención.

---

### ℹ️ Severidad: INFO

**Endpoints:** get-groups, get-group-by-id, get-group-members

**Problema:** Los endpoints de lectura no filtran por status del grupo. Esto significa que devuelven grupos tanto activos como inactivos. Puede ser intencional, pero debería estar documentado explícitamente en la API.

**Recomendación:** Considerar agregar query param opcional '?status=active' o documentar claramente que devuelve todos los status.

---

## Apéndice: Estructura de StandardResponse

Todos los endpoints de este servicio devuelven la siguiente estructura estándar:

```csharp
public class StandardResponse<T>
{
    public int StatusCode { get; set; }
    public string IntOpCode { get; set; }
    public string Message { get; set; }
    public T[] Data { get; set; } = Array.Empty<T>();
}
```

**Notas importantes:**
- `StatusCode`: Código HTTP estándar (200, 201, 400, 401, 403, 404, 409, 500)
- `IntOpCode`: Código interno de operación para tracking específico (formato: `EGRSSS###` donde SSS es identificador y ### es status code)
- `Message`: Mensaje descriptivo en inglés
- `Data`: Siempre es un array, incluso si está vacío o contiene un solo elemento

---

## Resumen de IntOpCodes

| IntOpCode | Descripción | Status Code |
|-----------|-------------|-------------|
| SGRCR201 | Group created successfully | 201 |
| SGRMB200 | Group member added/removed successfully | 200 |
| SGRRD200 | Groups/Group/Members retrieved successfully | 200 |
| SGRUP200 | Group updated successfully | 200 |
| SGRDL200 | Group deactivated successfully | 200 |
| EGRAU401 | Unauthorized - Invalid/missing auth | 401 |
| EGRFB403 | Forbidden - No permission | 403 |
| EGRNF404 | Not Found | 404 |
| EGRCF409 | Conflict | 409 |
| EGRBR400 | Bad Request | 400 |
| EGRIN500 | Internal Server Error | 500 |

---

**Documentación generada a partir del código fuente del proyecto Groups Microservice**

**Fecha:** 2026-04-06

**Versión del servicio:** Inferida del código actual
