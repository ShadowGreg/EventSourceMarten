create sequence mt_events_sequence;

alter sequence mt_events_sequence owner to postgres;

create table if not exists mt_doc_deadletterevent
(
    id               uuid                                                                                          not null
        constraint pkey_mt_doc_deadletterevent_id
            primary key,
    data             jsonb                                                                                         not null,
    mt_last_modified timestamp with time zone default transaction_timestamp(),
    mt_version       uuid                     default (md5(((random())::text || (clock_timestamp())::text)))::uuid not null,
    mt_dotnet_type   varchar
);

alter table mt_doc_deadletterevent
    owner to postgres;

create table if not exists mt_streams
(
    id               uuid                                   not null
        constraint pkey_mt_streams_id
            primary key,
    type             varchar,
    version          bigint,
    timestamp        timestamp with time zone default now() not null,
    snapshot         jsonb,
    snapshot_version integer,
    created          timestamp with time zone default now() not null,
    tenant_id        varchar                  default '*DEFAULT*'::character varying,
    is_archived      boolean                  default false
);

alter table mt_streams
    owner to postgres;

create table if not exists mt_events
(
    seq_id         bigint                                                                                     not null
        constraint pkey_mt_events_seq_id
            primary key,
    id             uuid                                                                                       not null,
    stream_id      uuid
        constraint fkey_mt_events_stream_id
            references mt_streams
            on delete cascade,
    version        bigint                                                                                     not null,
    data           jsonb                                                                                      not null,
    type           varchar(500)                                                                               not null,
    timestamp      timestamp with time zone default '2026-01-06 12:05:16.991122+00'::timestamp with time zone not null,
    tenant_id      varchar                  default '*DEFAULT*'::character varying,
    mt_dotnet_type varchar,
    is_archived    boolean                  default false
);

alter table mt_events
    owner to postgres;

alter sequence mt_events_sequence owned by mt_events.seq_id;

create unique index if not exists pk_mt_events_stream_and_version
    on mt_events (stream_id, version);

create table if not exists mt_event_progression
(
    name         varchar not null
        constraint pk_mt_event_progression
            primary key,
    last_seq_id  bigint,
    last_updated timestamp with time zone default transaction_timestamp()
);

alter table mt_event_progression
    owner to postgres;

create table if not exists mt_doc_driverhistoryitem
(
    id               uuid                                                                                          not null
        constraint pkey_mt_doc_driverhistoryitem_id
            primary key,
    data             jsonb                                                                                         not null,
    mt_last_modified timestamp with time zone default transaction_timestamp(),
    mt_version       uuid                     default (md5(((random())::text || (clock_timestamp())::text)))::uuid not null,
    mt_dotnet_type   varchar,
    driver_id        uuid,
    at               timestamp with time zone,
    message_ru       varchar,
    message_en       varchar
);

alter table mt_doc_driverhistoryitem
    owner to postgres;

create index if not exists mt_doc_driverhistoryitem_idx_driver_id
    on mt_doc_driverhistoryitem (driver_id);

create index if not exists mt_doc_driverhistoryitem_idx_at
    on mt_doc_driverhistoryitem (at);

create index if not exists mt_doc_driverhistoryitem_idx_message_ru
    on mt_doc_driverhistoryitem (message_ru);

create index if not exists mt_doc_driverhistoryitem_idx_message_en
    on mt_doc_driverhistoryitem (message_en);

create table if not exists mt_doc_driver
(
    id               uuid                               not null
        constraint pkey_mt_doc_driver_id
            primary key,
    data             jsonb                              not null,
    mt_last_modified timestamp with time zone default transaction_timestamp(),
    mt_dotnet_type   varchar,
    mt_version       integer                  default 0 not null,
    name             varchar,
    license_number   varchar,
    updated_at       timestamp with time zone,
    is_deleted       boolean
);

alter table mt_doc_driver
    owner to postgres;

create index if not exists mt_doc_driver_idx_name
    on mt_doc_driver (name);

create index if not exists mt_doc_driver_idx_license_number
    on mt_doc_driver (license_number);

create index if not exists mt_doc_driver_idx_updated_at
    on mt_doc_driver (updated_at);

create index if not exists mt_doc_driver_idx_is_deleted
    on mt_doc_driver (is_deleted);

create or replace function mt_immutable_timestamp(value text) returns timestamp without time zone
    immutable
    language sql
as
$$
select value::timestamp

$$;

alter function mt_immutable_timestamp(text) owner to postgres;

create or replace function mt_immutable_timestamptz(value text) returns timestamp with time zone
    immutable
    language sql
as
$$
select value::timestamptz

$$;

alter function mt_immutable_timestamptz(text) owner to postgres;

create or replace function mt_immutable_time(value text) returns time without time zone
    immutable
    language sql
as
$$
select value::time

$$;

alter function mt_immutable_time(text) owner to postgres;

create or replace function mt_immutable_date(value text) returns date
    immutable
    language sql
as
$$
select value::date

$$;

alter function mt_immutable_date(text) owner to postgres;

create or replace function mt_grams_vector(text, use_unaccent boolean DEFAULT false) returns tsvector
    immutable
    strict
    language plpgsql
as
$$
BEGIN
RETURN (SELECT array_to_string(event_source.mt_grams_array($1, use_unaccent), ' ') ::tsvector);
END
$$;

alter function mt_grams_vector(text, boolean) owner to postgres;

create or replace function mt_grams_query(text, use_unaccent boolean DEFAULT false) returns tsquery
    immutable
    strict
    language plpgsql
as
$$
BEGIN
RETURN (SELECT array_to_string(event_source.mt_grams_array($1, use_unaccent), ' & ') ::tsquery);
END
$$;

alter function mt_grams_query(text, boolean) owner to postgres;

create or replace function mt_grams_array(words text, use_unaccent boolean DEFAULT false) returns text[]
    immutable
    strict
    language plpgsql
as
$$
        DECLARE
result text[];
        DECLARE
word text;
        DECLARE
clean_word text;
BEGIN
                FOREACH
word IN ARRAY string_to_array(words, ' ')
                LOOP
                     clean_word = regexp_replace(event_source.mt_safe_unaccent(use_unaccent, word), '[^a-zA-Z0-9]+', '','g');
FOR i IN 1 .. length(clean_word)
                     LOOP
                         result := result || quote_literal(substr(lower(clean_word), i, 1));
                         result
:= result || quote_literal(substr(lower(clean_word), i, 2));
                         result
:= result || quote_literal(substr(lower(clean_word), i, 3));
END LOOP;
END LOOP;

RETURN ARRAY(SELECT DISTINCT e FROM unnest(result) AS a(e) ORDER BY e);
END;
$$;

alter function mt_grams_array(text, boolean) owner to postgres;

create or replace function mt_jsonb_append(jsonb, text[], jsonb, boolean, jsonb DEFAULT NULL::jsonb) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    val ALIAS FOR $3;
    if_not_exists ALIAS FOR $4;
    patch_expression ALIAS FOR $5;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
        CASE
            WHEN NOT if_not_exists THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            WHEN patch_expression IS NULL AND jsonb_typeof(val) = 'object' AND NOT tmp_value @> jsonb_build_array(val) THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            WHEN patch_expression IS NULL AND jsonb_typeof(val) <> 'object' AND NOT tmp_value @> val THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            WHEN patch_expression IS NOT NULL AND jsonb_typeof(patch_expression) = 'array' AND jsonb_array_length(patch_expression) = 0 THEN
                retval = jsonb_set(retval, location, tmp_value || val, FALSE);
            ELSE NULL;
            END CASE;
    END IF;
    RETURN retval;
END;
$$;

alter function mt_jsonb_append(jsonb, text[], jsonb, boolean, jsonb) owner to postgres;

create or replace function mt_jsonb_copy(jsonb, text[], text[]) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    src_path ALIAS FOR $2;
    dst_path ALIAS FOR $3;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> src_path;
    retval = event_source.mt_jsonb_fix_null_parent(retval, dst_path);
    RETURN jsonb_set(retval, dst_path, tmp_value::jsonb, TRUE);
END;
$$;

alter function mt_jsonb_copy(jsonb, text[], text[]) owner to postgres;

create or replace function mt_jsonb_duplicate(jsonb, text[], jsonb) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    targets ALIAS FOR $3;
    tmp_value jsonb;
    target_path text[];
    target text;
BEGIN
    FOR target IN SELECT jsonb_array_elements_text(targets)
    LOOP
        target_path = event_source.mt_jsonb_path_to_array(target, '\.');
        retval = event_source.mt_jsonb_copy(retval, location, target_path);
    END LOOP;

    RETURN retval;
END;
$$;

alter function mt_jsonb_duplicate(jsonb, text[], jsonb) owner to postgres;

create or replace function mt_jsonb_fix_null_parent(jsonb, text[]) returns jsonb
    language plpgsql
as
$$
DECLARE
retval ALIAS FOR $1;
    dst_path ALIAS FOR $2;
    dst_path_segment text[] = ARRAY[]::text[];
    dst_path_array_length integer;
    i integer = 1;
BEGIN
    dst_path_array_length = array_length(dst_path, 1);
    WHILE i <=(dst_path_array_length - 1)
    LOOP
        dst_path_segment = dst_path_segment || ARRAY[dst_path[i]];
        IF retval #> dst_path_segment IS NULL OR retval #> dst_path_segment = 'null'::jsonb THEN
            retval = jsonb_set(retval, dst_path_segment, '{}'::jsonb, TRUE);
        END IF;
        i = i + 1;
    END LOOP;

    RETURN retval;
END;
$$;

alter function mt_jsonb_fix_null_parent(jsonb, text[]) owner to postgres;

create or replace function mt_jsonb_increment(jsonb, text[], numeric) returns jsonb
    language plpgsql
as
$$
DECLARE
retval ALIAS FOR $1;
    location ALIAS FOR $2;
    increment_value ALIAS FOR $3;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NULL THEN
        tmp_value = to_jsonb(0);
END IF;

RETURN jsonb_set(retval, location, to_jsonb(tmp_value::numeric + increment_value), TRUE);
END;
$$;

alter function mt_jsonb_increment(jsonb, text[], numeric) owner to postgres;

create or replace function mt_jsonb_insert(jsonb, text[], jsonb, integer, boolean, jsonb DEFAULT NULL::jsonb) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    val ALIAS FOR $3;
    elm_index ALIAS FOR $4;
    if_not_exists ALIAS FOR $5;
    patch_expression ALIAS FOR $6;
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
        IF elm_index IS NULL THEN
            elm_index = jsonb_array_length(tmp_value) + 1;
        END IF;
        CASE
            WHEN NOT if_not_exists THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            WHEN patch_expression IS NULL AND jsonb_typeof(val) = 'object' AND NOT tmp_value @> jsonb_build_array(val) THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            WHEN patch_expression IS NULL AND jsonb_typeof(val) <> 'object' AND NOT tmp_value @> val THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            WHEN patch_expression IS NOT NULL AND jsonb_typeof(patch_expression) = 'array' AND jsonb_array_length(patch_expression) = 0 THEN
                retval = jsonb_insert(retval, location || elm_index::text, val);
            ELSE NULL;
        END CASE;
    END IF;
    RETURN retval;
END;
$$;

alter function mt_jsonb_insert(jsonb, text[], jsonb, integer, boolean, jsonb) owner to postgres;

create or replace function mt_jsonb_move(jsonb, text[], text) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    src_path ALIAS FOR $2;
    dst_name ALIAS FOR $3;
    dst_path text[];
    tmp_value jsonb;
BEGIN
    tmp_value = retval #> src_path;
    retval = retval #- src_path;
    dst_path = src_path;
    dst_path[array_length(dst_path, 1)] = dst_name;
    retval = event_source.mt_jsonb_fix_null_parent(retval, dst_path);
    RETURN jsonb_set(retval, dst_path, tmp_value, TRUE);
END;
$$;

alter function mt_jsonb_move(jsonb, text[], text) owner to postgres;

create or replace function mt_jsonb_path_to_array(text, character) returns text[]
    language plpgsql
as
$$
DECLARE
    location ALIAS FOR $1;
    regex_pattern ALIAS FOR $2;
BEGIN
RETURN regexp_split_to_array(location, regex_pattern)::text[];
END;
$$;

alter function mt_jsonb_path_to_array(text, char) owner to postgres;

create or replace function mt_jsonb_remove(jsonb, text[], jsonb) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    location ALIAS FOR $2;
    val ALIAS FOR $3;
    tmp_value jsonb;
    tmp_remove jsonb;
    patch_remove jsonb;
BEGIN
    tmp_value = retval #> location;
    IF tmp_value IS NOT NULL AND jsonb_typeof(tmp_value) = 'array' THEN
        IF jsonb_typeof(val) = 'array' THEN
            tmp_remove = val;
        ELSE
            tmp_remove = jsonb_build_array(val);
        END IF;

        FOR patch_remove IN SELECT * FROM jsonb_array_elements(tmp_remove)
        LOOP
            tmp_value =(SELECT jsonb_agg(elem)
            FROM jsonb_array_elements(tmp_value) AS elem
            WHERE elem <> patch_remove);
        END LOOP;

        IF tmp_value IS NULL THEN
            tmp_value = '[]'::jsonb;
        END IF;
    END IF;
    RETURN jsonb_set(retval, location, tmp_value, FALSE);
END;
$$;

alter function mt_jsonb_remove(jsonb, text[], jsonb) owner to postgres;

create or replace function mt_jsonb_patch(jsonb, jsonb) returns jsonb
    language plpgsql
as
$$
DECLARE
    retval ALIAS FOR $1;
    patchset ALIAS FOR $2;
    patch jsonb;
    patch_path text[];
    patch_expression jsonb;
    value jsonb;
BEGIN
    FOR patch IN SELECT * from jsonb_array_elements(patchset)
    LOOP
        patch_path = event_source.mt_jsonb_path_to_array((patch->>'path')::text, '\.');

        patch_expression = null;
        IF (patch->>'type') IN ('remove', 'append_if_not_exists', 'insert_if_not_exists') AND (patch->>'expression') IS NOT NULL THEN
            patch_expression = jsonb_path_query_array(retval #> patch_path, (patch->>'expression')::jsonpath);
        END IF;

        CASE patch->>'type'
            WHEN 'set' THEN
                retval = jsonb_set(retval, patch_path, (patch->'value')::jsonb, TRUE);
            WHEN 'delete' THEN
                retval = retval#-patch_path;
            WHEN 'append' THEN
                retval = event_source.mt_jsonb_append(retval, patch_path, (patch->'value')::jsonb, FALSE);
            WHEN 'append_if_not_exists' THEN
                retval = event_source.mt_jsonb_append(retval, patch_path, (patch->'value')::jsonb, TRUE, patch_expression);
            WHEN 'insert' THEN
                retval = event_source.mt_jsonb_insert(retval, patch_path, (patch->'value')::jsonb, (patch->>'index')::integer, FALSE);
            WHEN 'insert_if_not_exists' THEN
                retval = event_source.mt_jsonb_insert(retval, patch_path, (patch->'value')::jsonb, (patch->>'index')::integer, TRUE, patch_expression);
            WHEN 'remove' THEN
                retval = event_source.mt_jsonb_remove(retval, patch_path, COALESCE(patch_expression, (patch->'value')::jsonb));
            WHEN 'duplicate' THEN
                retval = event_source.mt_jsonb_duplicate(retval, patch_path, (patch->'targets')::jsonb);
            WHEN 'rename' THEN
                retval = event_source.mt_jsonb_move(retval, patch_path, (patch->>'to')::text);
            WHEN 'increment' THEN
                retval = event_source.mt_jsonb_increment(retval, patch_path, (patch->>'increment')::numeric);
            WHEN 'increment_float' THEN
                retval = event_source.mt_jsonb_increment(retval, patch_path, (patch->>'increment')::numeric);
            ELSE NULL;
        END CASE;
    END LOOP;
    RETURN retval;
END;
$$;

alter function mt_jsonb_patch(jsonb, jsonb) owner to postgres;

create or replace function mt_safe_unaccent(use_unaccent boolean, word text) returns text
    immutable
    strict
    language plpgsql
as
$$
BEGIN
IF use_unaccent THEN
    RETURN unaccent(word);
ELSE
    RETURN word;
END IF;
END;
$$;

alter function mt_safe_unaccent(boolean, text) owner to postgres;

create or replace function mt_upsert_deadletterevent(doc jsonb, docdotnettype character varying, docid uuid, docversion uuid) returns uuid
    language plpgsql
as
$$
DECLARE
  final_version uuid;
BEGIN
INSERT INTO event_source.mt_doc_deadletterevent ("data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (doc, docDotNetType, docId, docVersion, transaction_timestamp())
  ON CONFLICT (id)
  DO UPDATE SET "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp();

  SELECT mt_version FROM event_source.mt_doc_deadletterevent into final_version WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_upsert_deadletterevent(jsonb, varchar, uuid, uuid) owner to postgres;

create or replace function mt_insert_deadletterevent(doc jsonb, docdotnettype character varying, docid uuid, docversion uuid) returns uuid
    language plpgsql
as
$$
BEGIN
INSERT INTO event_source.mt_doc_deadletterevent ("data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (doc, docDotNetType, docId, docVersion, transaction_timestamp());

  RETURN docVersion;
END;
$$;

alter function mt_insert_deadletterevent(jsonb, varchar, uuid, uuid) owner to postgres;

create or replace function mt_update_deadletterevent(doc jsonb, docdotnettype character varying, docid uuid, docversion uuid) returns uuid
    language plpgsql
as
$$
DECLARE
  final_version uuid;
BEGIN
  UPDATE event_source.mt_doc_deadletterevent SET "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp() where id = docId;

  SELECT mt_version FROM event_source.mt_doc_deadletterevent into final_version WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_update_deadletterevent(jsonb, varchar, uuid, uuid) owner to postgres;

create or replace function mt_mark_event_progression(name character varying, last_encountered bigint) returns void
    language plpgsql
as
$$
BEGIN
INSERT INTO event_source.mt_event_progression (name, last_seq_id, last_updated)
VALUES (name, last_encountered, transaction_timestamp())
ON CONFLICT ON CONSTRAINT pk_mt_event_progression
    DO
UPDATE SET last_seq_id = last_encountered, last_updated = transaction_timestamp();

END;

$$;

alter function mt_mark_event_progression(varchar, bigint) owner to postgres;

create or replace function mt_archive_stream(streamid uuid) returns void
    language plpgsql
as
$$
BEGIN
  update event_source.mt_streams set is_archived = TRUE where id = streamid ;
  update event_source.mt_events set is_archived = TRUE where stream_id = streamid ;
END;
$$;

alter function mt_archive_stream(uuid) owner to postgres;

create or replace function mt_quick_append_events(stream uuid, stream_type character varying, tenantid character varying, event_ids uuid[], event_types character varying[], dotnet_types character varying[], bodies jsonb[]) returns integer[]
    language plpgsql
as
$$
DECLARE
	event_version int;
	event_type varchar;
	event_id uuid;
	body jsonb;
	index int;
	seq int;
    actual_tenant varchar;
	return_value int[];
BEGIN
	select version into event_version from event_source.mt_streams where id = stream;
	if event_version IS NULL then
		event_version = 0;
		insert into event_source.mt_streams (id, type, version, timestamp, tenant_id) values (stream, stream_type, 0, now(), tenantid);
    else
        if tenantid IS NOT NULL then
            select tenant_id into actual_tenant from event_source.mt_streams where id = stream;
            if actual_tenant != tenantid then
                RAISE EXCEPTION 'The tenantid does not match the existing stream';
            end if;
        end if;
	end if;

	index := 1;
	return_value := ARRAY[event_version + array_length(event_ids, 1)];

	foreach event_id in ARRAY event_ids
	loop
	    seq := nextval('event_source.mt_events_sequence');
		return_value := array_append(return_value, seq);

	    event_version := event_version + 1;
		event_type = event_types[index];
		body = bodies[index];

		insert into event_source.mt_events
			(seq_id, id, stream_id, version, data, type, tenant_id, timestamp, mt_dotnet_type, is_archived)
		values
			(seq, event_id, stream, event_version, body, event_type, tenantid, (now() at time zone 'utc'), dotnet_types[index], FALSE);

		index := index + 1;
	end loop;

	update event_source.mt_streams set version = event_version, timestamp = now() where id = stream;

	return return_value;
END
$$;

alter function mt_quick_append_events(uuid, varchar, varchar, uuid[], character varying[], character varying[], jsonb[]) owner to postgres;

create or replace function mt_insert_driverhistoryitem(arg_at timestamp with time zone, arg_driver_id uuid, arg_message_en character varying, arg_message_ru character varying, doc jsonb, docdotnettype character varying, docid uuid, docversion uuid) returns uuid
    language plpgsql
as
$$
BEGIN
INSERT INTO event_source.mt_doc_driverhistoryitem ("at", "driver_id", "message_en", "message_ru", "data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (arg_at, arg_driver_id, arg_message_en, arg_message_ru, doc, docDotNetType, docId, docVersion, transaction_timestamp());

  RETURN docVersion;
END;
$$;

alter function mt_insert_driverhistoryitem(timestamp with time zone, uuid, varchar, varchar, jsonb, varchar, uuid, uuid) owner to postgres;

create or replace function mt_upsert_driver(arg_is_deleted boolean, arg_license_number character varying, arg_name character varying, arg_updated_at timestamp with time zone, doc jsonb, docdotnettype character varying, docid uuid, revision integer) returns integer
    language plpgsql
as
$$
DECLARE
  final_version INTEGER;
  current_version INTEGER;
BEGIN

SELECT version into current_version FROM event_source.mt_streams WHERE id = docId ;
if revision = 0 then
  if current_version is not null then
    revision = current_version;
  else
    revision = 1;
  end if;
else
  if current_version is not null then
    if current_version > revision then
      return 0;
    end if;
  end if;
end if;

INSERT INTO event_source.mt_doc_driver ("is_deleted", "license_number", "name", "updated_at", "data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (arg_is_deleted, arg_license_number, arg_name, arg_updated_at, doc, docDotNetType, docId, revision, transaction_timestamp())
  ON CONFLICT (id)
  DO UPDATE SET "is_deleted" = arg_is_deleted, "license_number" = arg_license_number, "name" = arg_name, "updated_at" = arg_updated_at, "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = revision, mt_last_modified = transaction_timestamp() where revision > event_source.mt_doc_driver.mt_version;

  SELECT mt_version into final_version FROM event_source.mt_doc_driver WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_upsert_driver(boolean, varchar, varchar, timestamp with time zone, jsonb, varchar, uuid, integer) owner to postgres;

create or replace function mt_insert_driver(arg_is_deleted boolean, arg_license_number character varying, arg_name character varying, arg_updated_at timestamp with time zone, doc jsonb, docdotnettype character varying, docid uuid, revision integer) returns integer
    language plpgsql
as
$$
BEGIN
INSERT INTO event_source.mt_doc_driver ("is_deleted", "license_number", "name", "updated_at", "data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (arg_is_deleted, arg_license_number, arg_name, arg_updated_at, doc, docDotNetType, docId, revision, transaction_timestamp());
  RETURN 1;
END;
$$;

alter function mt_insert_driver(boolean, varchar, varchar, timestamp with time zone, jsonb, varchar, uuid, integer) owner to postgres;

create or replace function mt_update_driver(arg_is_deleted boolean, arg_license_number character varying, arg_name character varying, arg_updated_at timestamp with time zone, doc jsonb, docdotnettype character varying, docid uuid, revision integer) returns integer
    language plpgsql
as
$$
DECLARE
  final_version INTEGER;
  current_version INTEGER;
BEGIN
  if revision <= 1 then
    SELECT mt_version FROM event_source.mt_doc_driver into current_version WHERE id = docId ;
    if current_version is not null then
      revision = current_version + 1;
    end if;
  end if;

  UPDATE event_source.mt_doc_driver SET "is_deleted" = arg_is_deleted, "license_number" = arg_license_number, "name" = arg_name, "updated_at" = arg_updated_at, "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = revision, mt_last_modified = transaction_timestamp() where revision > event_source.mt_doc_driver.mt_version and id = docId;

  SELECT mt_version FROM event_source.mt_doc_driver into final_version WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_update_driver(boolean, varchar, varchar, timestamp with time zone, jsonb, varchar, uuid, integer) owner to postgres;

create or replace function mt_overwrite_driver(arg_is_deleted boolean, arg_license_number character varying, arg_name character varying, arg_updated_at timestamp with time zone, doc jsonb, docdotnettype character varying, docid uuid, revision integer) returns integer
    language plpgsql
as
$$
DECLARE
  final_version INTEGER;
  current_version INTEGER;
BEGIN

  if revision = 0 then
    SELECT mt_version FROM event_source.mt_doc_driver into current_version WHERE id = docId ;
    if current_version is not null then
      revision = current_version + 1;
    else
      revision = 1;
    end if;
  end if;

  INSERT INTO event_source.mt_doc_driver ("is_deleted", "license_number", "name", "updated_at", "data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (arg_is_deleted, arg_license_number, arg_name, arg_updated_at, doc, docDotNetType, docId, revision, transaction_timestamp())
  ON CONFLICT (id)
  DO UPDATE SET "is_deleted" = arg_is_deleted, "license_number" = arg_license_number, "name" = arg_name, "updated_at" = arg_updated_at, "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = revision, mt_last_modified = transaction_timestamp();

  SELECT mt_version FROM event_source.mt_doc_driver into final_version WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_overwrite_driver(boolean, varchar, varchar, timestamp with time zone, jsonb, varchar, uuid, integer) owner to postgres;

create or replace function mt_upsert_driverhistoryitem(arg_at timestamp with time zone, arg_driver_id uuid, arg_message_en character varying, arg_message_ru character varying, doc jsonb, docdotnettype character varying, docid uuid, docversion uuid) returns uuid
    language plpgsql
as
$$
DECLARE
  final_version uuid;
BEGIN
INSERT INTO event_source.mt_doc_driverhistoryitem ("at", "driver_id", "message_en", "message_ru", "data", "mt_dotnet_type", "id", "mt_version", mt_last_modified) VALUES (arg_at, arg_driver_id, arg_message_en, arg_message_ru, doc, docDotNetType, docId, docVersion, transaction_timestamp())
  ON CONFLICT (id)
  DO UPDATE SET "at" = arg_at, "driver_id" = arg_driver_id, "message_en" = arg_message_en, "message_ru" = arg_message_ru, "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp();

  SELECT mt_version FROM event_source.mt_doc_driverhistoryitem into final_version WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_upsert_driverhistoryitem(timestamp with time zone, uuid, varchar, varchar, jsonb, varchar, uuid, uuid) owner to postgres;

create or replace function mt_update_driverhistoryitem(arg_at timestamp with time zone, arg_driver_id uuid, arg_message_en character varying, arg_message_ru character varying, doc jsonb, docdotnettype character varying, docid uuid, docversion uuid) returns uuid
    language plpgsql
as
$$
DECLARE
  final_version uuid;
BEGIN
  UPDATE event_source.mt_doc_driverhistoryitem SET "at" = arg_at, "driver_id" = arg_driver_id, "message_en" = arg_message_en, "message_ru" = arg_message_ru, "data" = doc, "mt_dotnet_type" = docDotNetType, "mt_version" = docVersion, mt_last_modified = transaction_timestamp() where id = docId;

  SELECT mt_version FROM event_source.mt_doc_driverhistoryitem into final_version WHERE id = docId ;
  RETURN final_version;
END;
$$;

alter function mt_update_driverhistoryitem(timestamp with time zone, uuid, varchar, varchar, jsonb, varchar, uuid, uuid) owner to postgres;


