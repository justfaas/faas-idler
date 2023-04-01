# FaaS Idler

This project looks at available metrics for a function and when idle, scales the deployment to zero replicas. If there's activity, it scales the deployment back to min replicas and lets the HPA scale further if necessary.

Although there's already a feature gate called [HPAScaleToZero](https://kubernetes.io/docs/reference/command-line-tools-reference/feature-gates/) that does exactly the same, it's still in alpha, which means that it won't be available in some managed environments. So until the feature gate reaches beta, this is its replacement.

> HPAScaleToZero: Enables setting minReplicas to 0 for HorizontalPodAutoscaler resources when using custom or external metrics.
