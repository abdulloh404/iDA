


@Tags(['api'])
library;

import 'package:flutter_test/flutter_test.dart';
import 'package:ida_mobile/api/client.dart';

void main() {
  final api = IdaApi();

  test('GET /healthz → ok', () async {
    expect(await api.health(), 'ok');
  });

  test('GET /hello → ข้อความทักทาย default', () async {
    final res = await api.hello('');
    expect(res.message, contains('World'));
    expect(res.serverTime.year, greaterThan(2000));
  });

  test('GET /hello?name=… → ชื่อภาษาไทยผ่าน query string ได้', () async {
    expect((await api.hello('พญาไท')).message, contains('พญาไท'));
  });

  test('ชื่อยาวเกิน 64 → error envelope 400 name_too_long', () async {
    expect(
      () => api.hello('ก' * 70),
      throwsA(isA<ApiException>()
          .having((e) => e.status, 'status', 400)
          .having((e) => e.code, 'code', 'name_too_long')),
    );
  });
}
