#!/bin/sh

export GOBIN=${GOBIN:-$(go env GOPATH)/bin}
$GOBIN/telemetrygen traces --otlp-insecure --traces $1
$GOBIN/telemetrygen metrics --duration 5s --otlp-insecure
read