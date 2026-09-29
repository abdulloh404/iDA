import 'dart:convert';
import 'dart:io' show Platform;

import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:http/http.dart' as http;


String get apiBase {
  const override = String.fromEnvironment('API_URL');
  if (override.isNotEmpty) return override;

  if (!kIsWeb && Platform.isAndroid) return 'http://10.0.2.2:3100';
  return 'http://localhost:3100';
}


class ApiException implements Exception {
  ApiException(this.status, this.code, this.message, {this.traceId});

  final int status;
  final String code;
  final String message;
  final String? traceId;

  @override
  String toString() => '[$status $code] $message';
}

Future<T> _get<T>(String path) async {
  final res = await http
      .get(Uri.parse('$apiBase$path'), headers: {'Accept': 'application/json'})
      .timeout(const Duration(seconds: 10));

  final body = res.body.isEmpty
      ? null
      : jsonDecode(utf8.decode(res.bodyBytes)) as Map<String, dynamic>;

  if (res.statusCode >= 400) {
    final err = body?['error'] as Map<String, dynamic>?;
    throw ApiException(
      res.statusCode,
      err?['code'] as String? ?? 'http_error',
      err?['message'] as String? ?? 'HTTP ${res.statusCode}',
      traceId: err?['traceId'] as String?,
    );
  }
  return body as T;
}


class HelloDto {
  HelloDto(this.message, this.serverTime);

  factory HelloDto.fromJson(Map<String, dynamic> json) => HelloDto(
        json['message'] as String,
        DateTime.parse(json['serverTime'] as String),
      );

  final String message;
  final DateTime serverTime;
}

class IdaApi {
  Future<String> health() async {
    final json = await _get<Map<String, dynamic>>('/healthz');
    return json['status'] as String;
  }

  Future<HelloDto> hello(String name) async {
    final query = name.isEmpty ? '' : '?name=${Uri.encodeQueryComponent(name)}';
    return HelloDto.fromJson(await _get<Map<String, dynamic>>('/hello$query'));
  }
}
