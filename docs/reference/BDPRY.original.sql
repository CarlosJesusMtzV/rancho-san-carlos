CREATE TABLE [Especie] (
  [id] int PRIMARY KEY,
  [nombre] string CHECK ([nombre] IN ('Bovino', 'Caprino')),
  [descripcion] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Raza] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [proposito] string,
  [id_especie] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Animal] (
  [id] int PRIMARY KEY,
  [identificador_visual] string,
  [arete_oficial] string UNIQUE,
  [fecha_nacimiento] date,
  [sexo] string,
  [etapa_productiva] string,
  [peso_actual] float,
  [id_raza] int,
  [id_lote] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [LoteAnimal] (
  [id] int PRIMARY KEY,
  [codigo_lote] string,
  [descripcion] string,
  [id_especie] int,
  [id_instalacion] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [RegistroGenetico] (
  [id] int PRIMARY KEY,
  [id_animal] int UNIQUE,
  [id_padre] int,
  [id_madre] int,
  [notas_geneticas] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Reproduccion] (
  [id] int PRIMARY KEY,
  [id_hembra] int,
  [id_macho] int,
  [fecha_evento] date,
  [tipo] string,
  [id_insumo_semen] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Parto] (
  [id] int PRIMARY KEY,
  [id_madre] int,
  [fecha_parto] date,
  [numero_crias] int,
  [complicaciones] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Cria] (
  [id] int PRIMARY KEY,
  [id_parto] int,
  [id_animal_cria] int UNIQUE,
  [peso_nacimiento] float,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [TipoInsumo] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [categoria] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Insumo] (
  [id] int PRIMARY KEY,
  [nombre_comercial] string,
  [principio_activo] string,
  [id_tipo_insumo] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [InventarioInsumo] (
  [id] int PRIMARY KEY,
  [id_insumo] int,
  [cantidad_stock] float,
  [unidad_medida] string,
  [fecha_caducidad] date,
  [id_almacen] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Dieta] (
  [id] int PRIMARY KEY,
  [nombre_dieta] string,
  [descripcion] string,
  [etapa_vida] string,
  [id_especie] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [RegistroAlimentacion] (
  [id] int PRIMARY KEY,
  [fecha_hora] datetime,
  [cantidad_servida] float,
  [id_lote] int,
  [id_dieta] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [EventoSanitario] (
  [id] int PRIMARY KEY,
  [id_animal] int,
  [fecha] date,
  [tipo_evento] string,
  [diagnostico] string,
  [id_empleado_vet] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Tratamiento] (
  [id] int PRIMARY KEY,
  [id_evento_sanitario] int,
  [id_insumo] int,
  [dosis_aplicada] string,
  [periodo_retiro_dias] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [PruebaLaboratorio] (
  [id] int PRIMARY KEY,
  [id_animal] int,
  [fecha_muestra] date,
  [tipo_prueba] string,
  [resultado] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [TipoProducto] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [ProductoTerminado] (
  [id] int PRIMARY KEY,
  [sku] string UNIQUE,
  [nombre_producto] string,
  [descripcion] string,
  [precio_unitario] decimal(10,2),
  [id_tipo_producto] int,
  [id_especie] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [InventarioProducto] (
  [id] int PRIMARY KEY,
  [id_producto_terminado] int,
  [cantidad_stock] float,
  [fecha_elaboracion] date,
  [fecha_caducidad] date,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [ProduccionLeche] (
  [id] int PRIMARY KEY,
  [id_animal] int,
  [fecha_ordeno] datetime,
  [litros_producidos] float,
  [id_lote_leche] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [ProcesoSacrificio] (
  [id] int PRIMARY KEY,
  [id_animal] int UNIQUE,
  [fecha_sacrificio] date,
  [peso_canal] float,
  [id_empleado_responsable] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Despiece] (
  [id] int PRIMARY KEY,
  [id_proceso_sacrificio] int,
  [id_producto_terminado] int,
  [peso_corte] float,
  [fecha_empaque] date,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Departamento] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Puesto] (
  [id] int PRIMARY KEY,
  [nombre_puesto] string,
  [salario_base] decimal(10,2),
  [id_departamento] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Empleado] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [apellido] string,
  [rfc] string UNIQUE,
  [fecha_contratacion] date,
  [id_puesto] int,
  [id_supervisor] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Nomina] (
  [id] int PRIMARY KEY,
  [id_empleado] int,
  [fecha_pago] date,
  [monto_bruto] decimal(10,2),
  [deducciones] decimal(10,2),
  [monto_neto] decimal(10,2),
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Asistencia] (
  [id] int PRIMARY KEY,
  [id_empleado] int,
  [fecha_hora_entrada] datetime,
  [fecha_hora_salida] datetime,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Capacitacion] (
  [id] int PRIMARY KEY,
  [nombre_curso] string,
  [fecha_capacitacion] date,
  [id_empleado] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [TipoCliente] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Cliente] (
  [id] int PRIMARY KEY,
  [nombre_fiscal] string,
  [rfc] string UNIQUE,
  [direccion] string,
  [email] string,
  [telefono] string,
  [id_tipo_cliente] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Pedido] (
  [id] int PRIMARY KEY,
  [id_cliente] int,
  [fecha_pedido] date,
  [fecha_entrega_estimada] date,
  [estado] string,
  [total_pedido] decimal(10,2),
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [DetallePedido] (
  [id] int PRIMARY KEY,
  [id_pedido] int,
  [id_producto_terminado] int,
  [cantidad] int,
  [precio_unitario_venta] decimal(10,2),
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Factura] (
  [id] int PRIMARY KEY,
  [id_pedido] int,
  [folio_fiscal] string UNIQUE,
  [fecha_emision] date,
  [subtotal] decimal(10,2),
  [impuestos] decimal(10,2),
  [total_factura] decimal(10,2),
  [estado_pago] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Pago] (
  [id] int PRIMARY KEY,
  [id_factura] int,
  [fecha_pago] date,
  [monto] decimal(10,2),
  [metodo_pago] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Envio] (
  [id] int PRIMARY KEY,
  [id_pedido] int,
  [id_vehiculo] int,
  [id_empleado_conductor] int,
  [fecha_salida] datetime,
  [fecha_entrega_real] datetime,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [TipoInstalacion] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Instalacion] (
  [id] int PRIMARY KEY,
  [nombre_ubicacion] string,
  [capacidad] float,
  [id_tipo_instalacion] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [CategoriaEquipo] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Equipo] (
  [id] int PRIMARY KEY,
  [nombre_equipo] string,
  [numero_serie] string UNIQUE,
  [fecha_compra] date,
  [id_categoria_equipo] int,
  [id_instalacion] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Mantenimiento] (
  [id] int PRIMARY KEY,
  [id_equipo] int,
  [fecha_mantenimiento] date,
  [tipo] string,
  [descripcion_falla] string,
  [costo_reparacion] decimal(10,2),
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Vehiculo] (
  [id] int PRIMARY KEY,
  [placas] string UNIQUE,
  [marca] string,
  [modelo] string,
  [capacidad_carga_kg] float,
  [tipo] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Proveedor] (
  [id] int PRIMARY KEY,
  [nombre_empresa] string,
  [rfc] string UNIQUE,
  [contacto_nombre] string,
  [email] string,
  [telefono] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [OrdenCompra] (
  [id] int PRIMARY KEY,
  [id_proveedor] int,
  [fecha_orden] date,
  [total_orden] decimal(10,2),
  [estado] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [DetalleOrdenCompra] (
  [id] int PRIMARY KEY,
  [id_orden_compra] int,
  [id_insumo] int,
  [cantidad_solicitada] float,
  [precio_compra] decimal(10,2),
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [CentroCosto] (
  [id] int PRIMARY KEY,
  [nombre] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Transaccion] (
  [id] int PRIMARY KEY,
  [id_cuenta_bancaria] int,
  [tipo] string,
  [monto] decimal(10,2),
  [fecha_transaccion] datetime,
  [referencia] string,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Gasto] (
  [id] int PRIMARY KEY,
  [descripcion] string,
  [monto] decimal(10,2),
  [fecha_gasto] date,
  [id_centro_costo] int,
  [id_orden_compra] int,
  [id_transaccion] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [CuentaBancaria] (
  [id] int PRIMARY KEY,
  [banco] string,
  [numero_cuenta] string UNIQUE,
  [saldo_actual] decimal(10,2),
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

CREATE TABLE [Usuario] (
  [id] int PRIMARY KEY,
  [username] string UNIQUE,
  [password_hash] string,
  [email] string,
  [id_empleado] int,
  [idUserCreate] int,
  [dateCreate] datetime,
  [idUserUpdate] int,
  [dateUpdate] datetime,
  [idUserDelete] int,
  [dateDelete] datetime,
  [status] int
)
GO

ALTER TABLE [Especie] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Especie] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Especie] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Raza] ADD FOREIGN KEY ([id_especie]) REFERENCES [Especie] ([id])
GO

ALTER TABLE [Raza] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Raza] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Raza] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Animal] ADD FOREIGN KEY ([id_raza]) REFERENCES [Raza] ([id])
GO

ALTER TABLE [Animal] ADD FOREIGN KEY ([id_lote]) REFERENCES [LoteAnimal] ([id])
GO

ALTER TABLE [Animal] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Animal] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Animal] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [LoteAnimal] ADD FOREIGN KEY ([id_especie]) REFERENCES [Especie] ([id])
GO

ALTER TABLE [LoteAnimal] ADD FOREIGN KEY ([id_instalacion]) REFERENCES [Instalacion] ([id])
GO

ALTER TABLE [LoteAnimal] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [LoteAnimal] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [LoteAnimal] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [RegistroGenetico] ADD FOREIGN KEY ([id_animal]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [RegistroGenetico] ADD FOREIGN KEY ([id_padre]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [RegistroGenetico] ADD FOREIGN KEY ([id_madre]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [RegistroGenetico] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [RegistroGenetico] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [RegistroGenetico] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Reproduccion] ADD FOREIGN KEY ([id_hembra]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [Reproduccion] ADD FOREIGN KEY ([id_macho]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [Reproduccion] ADD FOREIGN KEY ([id_insumo_semen]) REFERENCES [InventarioInsumo] ([id])
GO

ALTER TABLE [Reproduccion] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Reproduccion] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Reproduccion] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Parto] ADD FOREIGN KEY ([id_madre]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [Parto] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Parto] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Parto] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Cria] ADD FOREIGN KEY ([id_parto]) REFERENCES [Parto] ([id])
GO

ALTER TABLE [Cria] ADD FOREIGN KEY ([id_animal_cria]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [Cria] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Cria] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Cria] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoInsumo] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoInsumo] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoInsumo] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Insumo] ADD FOREIGN KEY ([id_tipo_insumo]) REFERENCES [TipoInsumo] ([id])
GO

ALTER TABLE [Insumo] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Insumo] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Insumo] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [InventarioInsumo] ADD FOREIGN KEY ([id_insumo]) REFERENCES [Insumo] ([id])
GO

ALTER TABLE [InventarioInsumo] ADD FOREIGN KEY ([id_almacen]) REFERENCES [Instalacion] ([id])
GO

ALTER TABLE [InventarioInsumo] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [InventarioInsumo] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [InventarioInsumo] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Dieta] ADD FOREIGN KEY ([id_especie]) REFERENCES [Especie] ([id])
GO

ALTER TABLE [Dieta] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Dieta] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Dieta] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [RegistroAlimentacion] ADD FOREIGN KEY ([id_lote]) REFERENCES [LoteAnimal] ([id])
GO

ALTER TABLE [RegistroAlimentacion] ADD FOREIGN KEY ([id_dieta]) REFERENCES [Dieta] ([id])
GO

ALTER TABLE [RegistroAlimentacion] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [RegistroAlimentacion] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [RegistroAlimentacion] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [EventoSanitario] ADD FOREIGN KEY ([id_animal]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [EventoSanitario] ADD FOREIGN KEY ([id_empleado_vet]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [EventoSanitario] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [EventoSanitario] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [EventoSanitario] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Tratamiento] ADD FOREIGN KEY ([id_evento_sanitario]) REFERENCES [EventoSanitario] ([id])
GO

ALTER TABLE [Tratamiento] ADD FOREIGN KEY ([id_insumo]) REFERENCES [Insumo] ([id])
GO

ALTER TABLE [Tratamiento] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Tratamiento] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Tratamiento] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [PruebaLaboratorio] ADD FOREIGN KEY ([id_animal]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [PruebaLaboratorio] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [PruebaLaboratorio] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [PruebaLaboratorio] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoProducto] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoProducto] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoProducto] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProductoTerminado] ADD FOREIGN KEY ([id_tipo_producto]) REFERENCES [TipoProducto] ([id])
GO

ALTER TABLE [ProductoTerminado] ADD FOREIGN KEY ([id_especie]) REFERENCES [Especie] ([id])
GO

ALTER TABLE [ProductoTerminado] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProductoTerminado] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProductoTerminado] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [InventarioProducto] ADD FOREIGN KEY ([id_producto_terminado]) REFERENCES [ProductoTerminado] ([id])
GO

ALTER TABLE [InventarioProducto] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [InventarioProducto] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [InventarioProducto] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProduccionLeche] ADD FOREIGN KEY ([id_animal]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [ProduccionLeche] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProduccionLeche] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProduccionLeche] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProcesoSacrificio] ADD FOREIGN KEY ([id_animal]) REFERENCES [Animal] ([id])
GO

ALTER TABLE [ProcesoSacrificio] ADD FOREIGN KEY ([id_empleado_responsable]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [ProcesoSacrificio] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProcesoSacrificio] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [ProcesoSacrificio] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Despiece] ADD FOREIGN KEY ([id_proceso_sacrificio]) REFERENCES [ProcesoSacrificio] ([id])
GO

ALTER TABLE [Despiece] ADD FOREIGN KEY ([id_producto_terminado]) REFERENCES [ProductoTerminado] ([id])
GO

ALTER TABLE [Despiece] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Despiece] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Despiece] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Departamento] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Departamento] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Departamento] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Puesto] ADD FOREIGN KEY ([id_departamento]) REFERENCES [Departamento] ([id])
GO

ALTER TABLE [Puesto] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Puesto] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Puesto] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Empleado] ADD FOREIGN KEY ([id_puesto]) REFERENCES [Puesto] ([id])
GO

ALTER TABLE [Empleado] ADD FOREIGN KEY ([id_supervisor]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [Empleado] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Empleado] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Empleado] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Nomina] ADD FOREIGN KEY ([id_empleado]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [Nomina] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Nomina] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Nomina] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Asistencia] ADD FOREIGN KEY ([id_empleado]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [Asistencia] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Asistencia] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Asistencia] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Capacitacion] ADD FOREIGN KEY ([id_empleado]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [Capacitacion] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Capacitacion] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Capacitacion] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoCliente] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoCliente] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoCliente] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Cliente] ADD FOREIGN KEY ([id_tipo_cliente]) REFERENCES [TipoCliente] ([id])
GO

ALTER TABLE [Cliente] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Cliente] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Cliente] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Pedido] ADD FOREIGN KEY ([id_cliente]) REFERENCES [Cliente] ([id])
GO

ALTER TABLE [Pedido] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Pedido] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Pedido] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [DetallePedido] ADD FOREIGN KEY ([id_pedido]) REFERENCES [Pedido] ([id])
GO

ALTER TABLE [DetallePedido] ADD FOREIGN KEY ([id_producto_terminado]) REFERENCES [ProductoTerminado] ([id])
GO

ALTER TABLE [DetallePedido] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [DetallePedido] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [DetallePedido] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Factura] ADD FOREIGN KEY ([id_pedido]) REFERENCES [Pedido] ([id])
GO

ALTER TABLE [Factura] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Factura] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Factura] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Pago] ADD FOREIGN KEY ([id_factura]) REFERENCES [Factura] ([id])
GO

ALTER TABLE [Pago] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Pago] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Pago] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Envio] ADD FOREIGN KEY ([id_pedido]) REFERENCES [Pedido] ([id])
GO

ALTER TABLE [Envio] ADD FOREIGN KEY ([id_vehiculo]) REFERENCES [Vehiculo] ([id])
GO

ALTER TABLE [Envio] ADD FOREIGN KEY ([id_empleado_conductor]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [Envio] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Envio] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Envio] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoInstalacion] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoInstalacion] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [TipoInstalacion] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Instalacion] ADD FOREIGN KEY ([id_tipo_instalacion]) REFERENCES [TipoInstalacion] ([id])
GO

ALTER TABLE [Instalacion] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Instalacion] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Instalacion] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CategoriaEquipo] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CategoriaEquipo] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CategoriaEquipo] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Equipo] ADD FOREIGN KEY ([id_categoria_equipo]) REFERENCES [CategoriaEquipo] ([id])
GO

ALTER TABLE [Equipo] ADD FOREIGN KEY ([id_instalacion]) REFERENCES [Instalacion] ([id])
GO

ALTER TABLE [Equipo] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Equipo] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Equipo] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Mantenimiento] ADD FOREIGN KEY ([id_equipo]) REFERENCES [Equipo] ([id])
GO

ALTER TABLE [Mantenimiento] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Mantenimiento] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Mantenimiento] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Vehiculo] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Vehiculo] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Vehiculo] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Proveedor] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Proveedor] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Proveedor] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [OrdenCompra] ADD FOREIGN KEY ([id_proveedor]) REFERENCES [Proveedor] ([id])
GO

ALTER TABLE [OrdenCompra] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [OrdenCompra] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [OrdenCompra] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [DetalleOrdenCompra] ADD FOREIGN KEY ([id_orden_compra]) REFERENCES [OrdenCompra] ([id])
GO

ALTER TABLE [DetalleOrdenCompra] ADD FOREIGN KEY ([id_insumo]) REFERENCES [Insumo] ([id])
GO

ALTER TABLE [DetalleOrdenCompra] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [DetalleOrdenCompra] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [DetalleOrdenCompra] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CentroCosto] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CentroCosto] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CentroCosto] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Transaccion] ADD FOREIGN KEY ([id_cuenta_bancaria]) REFERENCES [CuentaBancaria] ([id])
GO

ALTER TABLE [Transaccion] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Transaccion] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Transaccion] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Gasto] ADD FOREIGN KEY ([id_centro_costo]) REFERENCES [CentroCosto] ([id])
GO

ALTER TABLE [Gasto] ADD FOREIGN KEY ([id_orden_compra]) REFERENCES [OrdenCompra] ([id])
GO

ALTER TABLE [Gasto] ADD FOREIGN KEY ([id_transaccion]) REFERENCES [Transaccion] ([id])
GO

ALTER TABLE [Gasto] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Gasto] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Gasto] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CuentaBancaria] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CuentaBancaria] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [CuentaBancaria] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Usuario] ADD FOREIGN KEY ([id_empleado]) REFERENCES [Empleado] ([id])
GO

ALTER TABLE [Usuario] ADD FOREIGN KEY ([idUserCreate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Usuario] ADD FOREIGN KEY ([idUserUpdate]) REFERENCES [Usuario] ([id])
GO

ALTER TABLE [Usuario] ADD FOREIGN KEY ([idUserDelete]) REFERENCES [Usuario] ([id])
GO
