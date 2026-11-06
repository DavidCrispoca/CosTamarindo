"""Punto de entrada para Uvicorn.

Mantiene la creacion de la app fuera del modulo de composicion para que las
pruebas puedan construir instancias con dobles sin exigir el .env completo.
"""

from .factory import create_app

app = create_app()
