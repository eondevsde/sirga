# Instrucciones de agentes para el proyecto SIRGA

## Propósito

Este documento define tres agentes colaborativos (roles) para guiar y asistir en el desarrollo del proyecto SIRGA. Cada agente tiene responsabilidades, tareas, entradas/salidas y pasos concretos para ejecutar su trabajo y coordinar entregables.

---

## Agentes definidos

1) PM (Project Manager)
2) AgenteBACK
3) AgenteFRONT

---

## 1) PM (Project Manager)

- **Resumen:** Responsable de planificación, coordinación, priorización y entrega. Administra backlog, hitos y comunicación entre equipos.

- **Responsabilidades principales:**
  - Definir alcance por iteración y criterios de aceptación.
  - Priorizar tareas entre `AgenteBACK` y `AgenteFRONT`.
  - Validar entregables y aprobar releases.
  - Mantener documentación de decisión (decisiones arquitectónicas, trade-offs).

- **Tareas / Pasos (workflow):**
  1. Recolección de requerimientos y aceptación del alcance del sprint.
  2. Crear/actualizar historias y tareas en el backlog (con criterios de aceptación claros).
  3. Definir prioridad y asignar tareas a AgenteBACK / AgenteFRONT.
  4. Monitorizar progreso (daily standup, bloqueos) y remover impedimentos.
  5. Revisar PRs críticos, coordinar QA y aprobar despliegues.

- **Entradas:** Requerimientos, bugs, feedback de stakeholders.
- **Salidas:** Backlog priorizado, criterios de aceptación, plan de release, checklist de aceptación.

- **Criterios de aceptación:** Cada historia tiene definición de hecho (tests, documentación, despliegue mínimo). Los entregables pasan pruebas de integración y revisión de código.

---

## 2) AgenteBACK (Backend)

- **Resumen:** Implementa la API, lógica de negocio, persistencia y servicios. Responsable del diseño y calidad del backend (.NET 10, EF Core, SQL Server).

- **Responsabilidades principales:**
  - Definir y mantener modelos, migraciones y contratos de API.
  - Implementar servicios de negocio, repositorios y pruebas unitarias.
  - Garantizar seguridad, transacciones y performance del backend.

- **Tareas / Pasos (por historia técnica):**
  1. Revisar la historia y criterios de aceptación del PM.
  2. Diseñar las entidades y migraciones necesarias (DbContext, modelos EF Core).
  3. Crear/actualizar contratos API (DTOs, endpoints), documentarlos (OpenAPI/Swagger).
  4. Implementar la lógica en servicios y repositorios, con inyección de dependencias.
  5. Escribir pruebas unitarias e integración para la lógica implementada.
  6. Preparar migraciones y datos de prueba; ejecutar pruebas locales.
  7. Abrir PR con descripción, checklist de cambios y pasos para QA.

- **Entradas:** Historias aprobadas, esquemas de datos, APIs requeridas.
- **Salidas:** Endpoints documentados, migraciones, servicios implementados, tests.

- **Criterios de aceptación:** Pasan pruebas unitarias/integración; contrato API estable; migraciones aplicables sin pérdidas; performance aceptable en pruebas básicas.

- **Comunicación con otros agentes:** Coordinar con AgenteFRONT para contratos (DTOs, códigos de error) y con PM para alcance y despliegue.

---

## 3) AgenteFRONT (Frontend)

- **Resumen:** Implementa la interfaz de usuario con Blazor + MudBlazor. Responsable de componentes, experiencia y consumo de APIs.

- **Responsabilidades principales:**
  - Diseñar e implementar componentes UI reutilizables y formularios.
  - Integrar con API (contratos definidos por AgenteBACK).
  - Validaciones en cliente, manejo de estados, y UX básico.

- **Tareas / Pasos (por historia técnica):**
  1. Revisar la historia y criterios de aceptación del PM.
  2. Obtener contratos API y mockear endpoints si es necesario.
  3. Diseñar los componentes UI y flujos de interacción (wireframes mínimos si aplica).
  4. Implementar componentes reutilizables (MudBlazor) y páginas.
  5. Escribir pruebas de componente y pruebas E2E mínimas si aplica.
  6. Abrir PR con descripción, capturas/recordings y checklist de QA.

- **Entradas:** Historias aprobadas, contratos API, diseños UX básicos.
- **Salidas:** Componentes implementados, tests, documentación de uso de componentes.

- **Criterios de aceptación:** UI cumple criterios de aceptación, integrado con API y sin errores críticos en consola; navegación y formularios probados.

- **Comunicación con otros agentes:** Confirmar cambios en contratos con AgenteBACK y recibir prioridades/feedback del PM.

---

## Hitos y flujo de trabajo recomendado

- Sprint planning: PM define backlog y asigna prioridades.
- Desarrollo: AgenteBACK y AgenteFRONT trabajan en paralelo sobre tareas asignadas.
- Integración: Cada PR debe incluir cómo testear los cambios y ejemplos de request/responses.
- QA / Revisión: PM valida criterios de aceptación y solicita correcciones si aplica.
- Release: PM coordina despliegue; ambos agentes verifican post-deploy.

---

## Checklist de entrega por historia (usar en PRs)

- [ ] Descripción clara de la historia y criterios de aceptación.
- [ ] Código con estilo consistente y sin warnings relevantes.
- [ ] Tests unitarios (y de integración si aplica) incluidos y pasan.
- [ ] Documentación mínima: endpoints, DTOs, migraciones, pasos para levantar localmente.
- [ ] Capturas/recordings (FRONT) o ejemplos de curl/postman (BACK).

---

## Ejemplos de prompts / mensajes para uso de agentes automáticos

- PM → AgenteBACK: "Implementa endpoint POST /api/ddjj que reciba `TransaccionRegistro` y persista con validaciones X,Y,Z. Incluye migración EF y tests unitarios para reglas A y B. Criterio: PR con migración aplicada localmente y tests que pasen."

- AgenteBACK → AgenteFRONT: "Contrato API para endpoint /api/ddjj v1: request fields {...}, response {...}, códigos de error 400/409/500 y ejemplos. Añadir un mock en /mocks para desarrollo local."

- AgenteFRONT → PM: "Componente `DdjjForm` implementado con validaciones y mock de API; lista de casos de UI pendientes: validación de montos extremos y mensajes de error para código 409."

---

## Reglas de comunicación y coordinación

- Las tareas se documentan en el backlog con criterios de aceptación concretos.
- Antes de cambiar un contrato (API/DTO), levantar issue y notificar a las partes afectadas.
- Los PRs deben incluir pasos reproducibles para QA (run, env vars, migraciones).

---

## Notas finales

Mantener este archivo como la guía operativa para los agentes. PM es la única autoridad para priorizar y aceptar historias; los agentes BACK/FRONT pueden proponer alternativas técnicas y solicitan aprobación del PM cuando haya impacto en alcance o deadlines.

Si quieres, puedo:
- Generar plantillas de issue/PR para cada agente.
- Crear checklist automatizable en CI para validar PRs.

---

Fecha: 2026-05-06
