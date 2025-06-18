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
