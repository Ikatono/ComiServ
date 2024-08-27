# ComiServ

An educational project to create a comic server in Asp.Net Core and Entity Framework Core. Scans a library folder for comic files, and exposes an API to:

- Get/Set metadata for any comic file (author, tags, description, etc.)
- Search comics using any metadata
- Track read status for each user
- Serve entire comic files, or individual pages (with optional resizing and re-encoding)
- identify duplicate files in the library

API is thoroughly documented through Swagger. In progress is a web app that consumes the API and provides a convenient user interface.

(If this sounds like something you want to run on your machine, you should check out komga: https://github.com/gotson/komga)
