## Reglas de Implementación: Mercado Pago SDK para .NET 10

## 1. Contexto del Proyecto

- **País de Operación:** Argentina (Marketplace ID: MLA).
- **Moneda:** ARS (Pesos Argentinos).
- **Framework:** .NET 10 (C# 13).
- **Arquitectura:** Web API con Inyección de Dependencias y persistencia en SQL Server.

## 2. Requisitos de Integración (SDKs)

Para cada sugerencia de código, se deben seguir estos pilares:

### A. Server-side (Backend)

- **Biblioteca:** Utilizar exclusivamente el SDK oficial `mercadopago-sdk` v2.x para .NET.
- **Patrón:** Implementar servicios asíncronos (`Task<T>`) para evitar bloqueos de hilo.
- **Seguridad:** El `AccessToken` debe leerse desde `IConfiguration` o Secret Manager. Nunca Hardcoded.
- **Funcionalidades requeridas:**
  - Creación de Preferencias (Checkout Pro).
  - Gestión de Reembolsos y Contracargos.
  - Consulta de estado de transacciones mediante `PaymentClient`.

### B. Client-side (Frontend)

- **Biblioteca:** `MercadoPago.js` v2.
- **Seguridad:** Utilizar la `PublicKey` para la captura de datos sensibles y generación de tokens de tarjeta (Tokenización).

## 3. Lógica de Negocio y Flujo de Datos

Al generar código, la IA debe cumplir las siguientes reglas:

1. **Preferencia de Pago:** Los items deben incluir `Title`, `Quantity` y `UnitPrice`.
2. **Back URLs:** Configurar siempre las URLs de retorno (`Success`, `Failure`, `Pending`) apuntando al dominio de la aplicación.
3. **Webhooks / IPN:**
   - Crear un endpoint `POST` dedicado para recibir notificaciones de Mercado Pago.
   - El flujo debe ser: Recibir ID -> Consultar estado en Mercado Pago -> Actualizar Base de Datos SQL Server.

## 4. Estándares de Código

- Usar **Primary Constructors** y **File-scoped namespaces** (C# 12/13).
- Implementar manejo de excepciones robusto para errores de API (ej: `MPException`).
- Separar la lógica de Mercado Pago en una capa de `Services` independiente de los `Controllers`.

## 5. Pruebas (Modo Sandbox)

- Generar siempre ejemplos utilizando credenciales de prueba.
- Simular respuestas de error para validar la resiliencia del sistema.

--
content_section_with_media:

- title: SDKs
- message: Con las bibliotecas de SDKs de Mercado Pago, puedes reducir el tiempo de integración client-side y server-side.
- media_image: /sdk/library-media1.png

---

--- mini_landing_separator ---

> > > > Disponibilidad por país <<<<

---

## available_countries: mla, mlb, mlm, mco, mpe, mlu, mlc

---

bullet_section_with_media:

- title: Client-side
- type: normal
- message: Con MercadoPago.js o el SDK de React instalado, puedes realizar llamadas para el token de tarjetas y utilizar muchas otras funcionalidades en la integración.
- image: /sdk/library-media-2.png

---

---

bullet_section_with_media:

- title: Server-side
- type: reversed
- message: Al utilizar nuestros SDKs de backend, obtienes un acceso fácil a las funcionalidades server-side de nuestras soluciones. Esto te permite crear y consultar el estado de diversas transacciones, integrar pagos a través de tarjeta y otros métodos, además de realizar reembolsos o contracargos.
- image: /sdk/library-media-3.png

---
