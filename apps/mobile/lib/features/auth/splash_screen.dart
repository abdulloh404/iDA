import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../ui/components.dart';


class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => AnnotatedRegion<SystemUiOverlayStyle>(
        value: SystemUiOverlayStyle.light,
        child: Scaffold(
          body: IdaHeroSurface(
            child: Center(
              child: Column(mainAxisSize: MainAxisSize.min, children: [
                const IdaLogo.lockup(height: IdaSizes.logoLockup, reversed: true),
                const SizedBox(height: IdaSpace.s8),
                SizedBox.square(
                  dimension: IdaSizes.iconLg,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.5,
                    color: IdaColors.textInverse.withValues(alpha: 0.9),
                  ),
                ),
              ]),
            ),
          ),
        ),
      );
}
