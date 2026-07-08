FROM php:8.3-apache

RUN apt-get update \
    && apt-get install -y --no-install-recommends git unzip libzip-dev \
    && docker-php-ext-install pdo_mysql zip \
    && a2enmod rewrite headers \
    && rm -rf /var/lib/apt/lists/*

COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

WORKDIR /var/www/html
COPY composer.json composer.json
RUN composer install --no-dev --prefer-dist --no-interaction --no-progress --optimize-autoloader

COPY . .
COPY docker/apache.conf /etc/apache2/sites-available/000-default.conf

RUN mkdir -p runtime web/assets \
    && chown -R www-data:www-data runtime web/assets

EXPOSE 80
