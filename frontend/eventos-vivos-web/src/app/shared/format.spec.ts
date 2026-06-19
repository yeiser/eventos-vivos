import {
  aIsoConOffset,
  badgeAccion,
  badgeEstadoEvento,
  badgeEstadoReserva,
  etiquetaAccion,
  etiquetaEstadoEvento,
  etiquetaEstadoReserva,
  etiquetaTipo,
  formatearJson,
  limiteEntradas,
} from './format';

const enHoras = (h: number) => new Date(Date.now() + h * 3_600_000).toISOString();

describe('format helpers', () => {
  describe('etiquetas y badges', () => {
    it('traduce el tipo de evento', () => {
      expect(etiquetaTipo('conferencia')).toBe('Conferencia');
      expect(etiquetaTipo('taller')).toBe('Taller');
    });

    it('estado de evento: etiqueta y badge', () => {
      expect(etiquetaEstadoEvento('activo')).toBe('Activo');
      expect(badgeEstadoEvento('cancelado')).toBe('badge-light-danger');
    });

    it('estado de reserva: etiqueta y badge', () => {
      expect(etiquetaEstadoReserva('pendiente_pago')).toBe('Pendiente de pago');
      expect(badgeEstadoReserva('confirmada')).toBe('badge-light-success');
      expect(badgeEstadoReserva('perdida')).toBe('badge-light-danger');
    });

    it('acción de auditoría: etiqueta y badge', () => {
      expect(etiquetaAccion('crear')).toBe('Creación');
      expect(badgeAccion('eliminar')).toBe('badge-light-danger');
    });

    it('cae a un valor por defecto ante claves desconocidas', () => {
      expect(etiquetaTipo('otro' as never)).toBe('otro');
      expect(badgeEstadoEvento('xxx' as never)).toBe('badge-light');
      expect(badgeEstadoReserva('xxx' as never)).toBe('badge-light');
      expect(badgeAccion('xxx' as never)).toBe('badge-light');
    });
  });

  describe('limiteEntradas (composición A-02)', () => {
    it('devuelve 0 si faltan menos de 1 hora (RN04)', () => {
      expect(limiteEntradas(enHoras(0.5), 50, 100)).toBe(0);
    });

    it('limita a 5 si faltan menos de 24h (RF-03)', () => {
      expect(limiteEntradas(enHoras(2), 50, 100)).toBe(5);
    });

    it('limita a 10 si el precio supera 100 (RN05)', () => {
      expect(limiteEntradas(enHoras(48), 150, 100)).toBe(10);
    });

    it('toma la regla más restrictiva (min)', () => {
      // <24h (5) y precio>100 (10) -> gana 5
      expect(limiteEntradas(enHoras(2), 150, 100)).toBe(5);
    });

    it('respeta las entradas disponibles cuando son el límite menor', () => {
      expect(limiteEntradas(enHoras(48), 50, 3)).toBe(3);
    });
  });

  describe('aIsoConOffset', () => {
    it('produce un ISO 8601 con offset que preserva la hora de pared', () => {
      const iso = aIsoConOffset('2026-07-15T20:30');
      expect(iso).toMatch(/^2026-07-15T20:30:00[+-]\d{2}:\d{2}$/);
    });
  });

  describe('formatearJson', () => {
    it('indenta un JSON válido', () => {
      expect(formatearJson('{"a":1}')).toBe('{\n  "a": 1\n}');
    });

    it('devuelve el texto tal cual si no es JSON', () => {
      expect(formatearJson('no-json')).toBe('no-json');
    });

    it('devuelve cadena vacía ante null', () => {
      expect(formatearJson(null)).toBe('');
    });
  });
});
