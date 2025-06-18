# SUNAT RUC API 🚀🇵🇪

Solución en C# .NET que expone una API minimalista para consultar datos del RUC directamente desde la web de SUNAT. El captcha se resuelve automáticamente para facilitar la integración en sistemas propios.

## ✨ Características
- 🔍 Búsqueda por número de RUC, documento o razón social.
- ✅ Captcha resuelto en segundo plano.
- 🔧 Endpoints HTTP y servicio gRPC opcional.
- 🛡️ Cache en memoria y soporte para Redis.

## 🛠️ Requisitos
- .NET SDK 9.0 o superior
- Acceso a internet para restaurar paquetes
- Tesseract OCR instalado

## 🚀 Ejecución rápida
```bash
dotnet run --project SunatScraper.Api
```
La API quedará disponible en `http://localhost:5000/`.

## 📁 Endpoints principales
- `GET /` – Comprobación de funcionamiento.
- `GET /ruc/{ruc}` – Consulta por número de RUC.
- `GET /doc/{tipo}/{numero}` – Búsqueda por tipo y número de documento.
- `GET /doc/{tipo}/{numero}/lista` – Devuelve la "Relación de contribuyentes" para el documento indicado.
- `GET /rs?q={razon social}` – Búsqueda por nombre o razón social.

## 💻 Ejemplos de uso
### Consulta por RUC
```bash
curl http://localhost:5000/ruc/20100113774
```

### Búsqueda por documento (DNI)
```bash
curl http://localhost:5000/doc/1/73870570
```
### Obtener lista de resultados para un documento
```bash
curl http://localhost:5000/doc/1/73870570/lista
```

### Búsqueda por razón social
```bash
curl "http://localhost:5000/rs?q=ACME"
```

### Ejemplo con Redis activado
```bash
Redis=localhost:6379 dotnet run --project SunatScraper.Api
curl http://localhost:5000/ruc/20100113774
```

### Consulta vía gRPC
```bash
grpcurl -d '{"ruc":"20100113774"}' -plaintext localhost:5000 Sunat/GetByRuc
```

## 📄 Arquitectura
El proyecto se divide en tres componentes principales:
- **SunatScraper.Core** – Librería de dominio. Contiene la lógica de scraping, validación de entradas, resolución de captcha y la clase `SunatClient` que centraliza el acceso a SUNAT.
- **SunatScraper.Api** – Capa de presentación HTTP basada en Minimal API que publica los endpoints REST y configura las dependencias.
- **SunatScraper.Grpc** – Servicio gRPC opcional para escenarios donde se requiera comunicación binaria.

### Arquitectura utilizada
Se adopta una **arquitectura en capas** donde el núcleo de negocio se mantiene aislado en `SunatScraper.Core`. Las capas superiores consumen esta librería a través de *inyección de dependencias*, permitiendo cambiar el mecanismo de exposición (REST o gRPC) sin modificar el código del dominio.

### Patrones de diseño
- **Factory Method** en `SunatClient.Create` para configurar `HttpClient` y las opciones de caché.
- **Dependency Injection** para registrar servicios y mantener bajo acoplamiento.
- **Caching** en memoria y opcionalmente en Redis para optimizar consultas repetitivas.

### ¿Por qué C# .NET?
- Alto rendimiento y soporte multiplataforma.
- Amplio ecosistema de bibliotecas para HTTP, gRPC y manipulación de HTML.
- Facilidad de despliegue en contenedores o servidores Windows/Linux.


## 🗂 Estructura del proyecto
```text
.
├── README.md
├── SunatScraper.Api
│   ├── Program.cs
│   └── SunatScraper.Api.csproj
├── SunatScraper.Core
│   ├── Models
│   ├── Services
│   ├── SunatScraper.Core.csproj
│   └── Validation
└── SunatScraper.Grpc
    ├── Services
    ├── SunatScraper.Grpc.csproj
    └── sunat.proto
```

## ⚠️ Advertencia
El portal de SUNAT puede cambiar o tener restricciones de acceso. Este código se comparte con fines educativos y debe usarse respetando los términos de SUNAT.

## 🔘 Solución a error "Captcha request failed: 401 Unauthorized"
Si al realizar una consulta la API muestra `Captcha request failed: 401 Unauthorized`, revisa lo siguiente:

1. Usa la última versión del proyecto. La clase `SunatSecurity` simula un navegador real estableciendo `User-Agent`, `Referer`, `Accept` y `Accept-Language`. También incluye el valor aleatorio `nmagic`/`numRnd` que SUNAT valida para permitir la descarga.
2. Previamente se debe cargar la página `FrameCriterioBusquedaWeb.jsp` para obtener las cookies de sesión. El método `SunatClient.SendAsync` ya realiza esta petición antes de solicitar el captcha.
3. Verifica que tu conexión permita acceder a `e-consultaruc.sunat.gob.pe`; un cortafuego o proxy podría bloquear la descarga del captcha o descartar las cookies.
4. Asegúrate de tener instalado Tesseract OCR para que el captcha se resuelva automáticamente. Si Tesseract no está disponible se solicitará ingresarlo manualmente.
5. A partir de la versión actual la clase `SunatSecurity` detecta los códigos `401 Unauthorized` y `404 Not Found` devolviendo un captcha vacío cuando SUNAT lo omite, evitando que se genere una excepción.

Tras comprobar estos puntos la API debería responder correctamente a las consultas `/ruc/{ruc}`.
