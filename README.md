# SUNAT RUC API

Este proyecto proporciona una API minimalista en C# para consultar datos del RUC a través de la página de SUNAT resolviendo automáticamente el captcha.

## Requisitos
- .NET SDK 9.0 o superior.
- Acceso a internet para restaurar paquetes NuGet.
- Tesseract OCR disponible en el sistema.

## Ejecución
```bash
dotnet run --project SunatScraper.Api
```
La API quedará disponible en `http://localhost:5000/`.

## Endpoints principales
- `GET /` – Comprobación de funcionamiento.
- `GET /ruc/{ruc}` – Consulta por número de RUC.
- `GET /doc/{tipo}/{numero}` – Búsqueda por tipo y número de documento (ejemplo de DNI: `/doc/1/73870570`).
- `GET /rs?q={razon social}` – Búsqueda por nombre o razón social.

## Estructura del proyecto
- **SunatScraper.Api** – Aplicación web que expone los endpoints REST.
- **SunatScraper.Core** – Biblioteca con la lógica de scraping, validaciones y resolución de captcha.
- **SunatScraper.Grpc** – Servicio gRPC de ejemplo para futuras integraciones.

## Advertencia
El acceso al portal de SUNAT puede verse afectado por restricciones de red o cambios en la página oficial. Este proyecto se comparte con fines educativos; úselo respetando los términos y condiciones de SUNAT.
