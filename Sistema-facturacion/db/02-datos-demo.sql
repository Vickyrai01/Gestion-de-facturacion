-- Datos de demostración. SOLO para desarrollo local.
--
-- Las cuentas tienen hash BCrypt (factor 12), el mismo formato que genera el
-- legacy (PasswordSeguridad.cs). Así la clase 6 puede mostrar la migración al
-- hash de Identity en el primer login.
--
--   admin    / admin-clase-6!     (ADMIN)
--   operador / operador-clase-6!  (OPERADOR)
--
-- Para volver a probar la migración, restaurar el hash BCrypt con un UPDATE.

ALTER SESSION SET CONTAINER = FREEPDB1;

INSERT INTO facturacion_app.usuarios (nombreusuario, passwordhash, rol, activo)
VALUES ('admin', '$2y$12$WKnraLu4Ppc/uXGSiZ320u1qPUkYZJEdpDA5zgx79jcVMZKA/3sp6', 'ADMIN', 1);

INSERT INTO facturacion_app.usuarios (nombreusuario, passwordhash, rol, activo)
VALUES ('operador', '$2y$12$4fMvPNpTRRGqaq33.sO5PuIE0XEX56QaUu0Co84xYVA7LvFEryQ9e', 'OPERADOR', 1);

INSERT INTO facturacion_app.clientes (nombre, apellido, documento, email, activo)
VALUES ('Ana', 'Perez', '30111222', 'ana.perez@example.com', 1);

INSERT INTO facturacion_app.clientes (nombre, apellido, documento, email, activo)
VALUES ('Juan', 'Gomez', '28999888', 'juan.gomez@example.com', 1);

INSERT INTO facturacion_app.productos (codigo, nombre, descripcion, preciounitario, stock, activo)
VALUES ('P-001', 'Resma A4', 'Resma de 500 hojas', 8500, 100, 1);

INSERT INTO facturacion_app.productos (codigo, nombre, descripcion, preciounitario, stock, activo)
VALUES ('P-002', 'Toner', 'Toner para impresora laser', 42000, 20, 1);

COMMIT;
