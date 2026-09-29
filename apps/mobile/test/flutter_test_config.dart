import 'dart:async';

import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';


Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  TestWidgetsFlutterBinding.ensureInitialized();
  Future<void> load(String family, List<String> files) async {
    final loader = FontLoader(family);
    for (final f in files) {
      loader.addFont(rootBundle.load('assets/fonts/$f'));
    }
    await loader.load();
  }

  const weights = ['Regular', 'Medium', 'SemiBold', 'Bold'];
  await load('NotoSansThai', [for (final w in weights) 'NotoSansThai-$w.ttf']);
  await load('NotoSans', [for (final w in weights) 'NotoSans-$w.ttf']);
  await load('Poppins', ['Poppins-SemiBold.ttf', 'Poppins-Bold.ttf']);
  await testMain();
}
